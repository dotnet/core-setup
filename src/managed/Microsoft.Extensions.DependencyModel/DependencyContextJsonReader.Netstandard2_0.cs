// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextJsonReader : IDependencyContextReader
    {
        private readonly IDictionary<string, string> _stringPool = new Dictionary<string, string>();

        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ArraySegment<byte> drained = ReadToEnd(stream);

            try
            {
                return ReadCore(drained.AsSpan(0, drained.Count), drained.Array);
            }
            catch
            {
                // Holds document content, clear it before returning it.
                drained.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(drained.Array);
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stringPool.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private const int UnseekableStreamInitialRentSize = 4096;

        private static ArraySegment<byte> ReadToEnd(Stream stream)
        {
            int written = 0;
            byte[] rented = null;

            try
            {
                long expectedLength = 0;

                if (stream.CanSeek)
                {
                    expectedLength = Math.Max(1L, stream.Length - stream.Position);
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                do
                {
                    if (expectedLength == 0 && rented.Length == written)
                    {
                        byte[] toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(checked(toReturn.Length * 2));
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, clearArray: true);
                    }

                    lastRead = stream.Read(rented, written, rented.Length - written);
                    written += lastRead;
                } while (lastRead > 0);

                return new ArraySegment<byte>(rented, 0, written);
            }
            catch
            {
                if (rented != null)
                {
                    // Holds document content, clear it before returning it.
                    rented.AsSpan(0, written).Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }
                throw;
            }
        }

        private DependencyContext ReadCore(ReadOnlySpan<byte> jsonData, byte[] rentedBuffer)
        {
            var jsonReader = new Utf8JsonReader(jsonData, isFinalBlock: true, state: default);

            jsonReader.ReadStartObject();

            var runtime = string.Empty;
            var framework = string.Empty;
            var isPortable = true;
            string runtimeTargetName = null;
            string runtimeSignature = null;

            CompilationOptions compilationOptions = null;
            List<Target> targets = null;
            Dictionary<string, LibraryStub> libraryStubs = null;
            List<RuntimeFallbacks> runtimeFallbacks = null;

            while (jsonReader.Read() && jsonReader.TokenType == JsonTokenType.PropertyName)
            {
                switch (jsonReader.GetStringValue())
                {
                    case DependencyContextStrings.RuntimeTargetPropertyName:
                        ReadRuntimeTarget(ref jsonReader, out runtimeTargetName, out runtimeSignature);
                        break;
                    case DependencyContextStrings.CompilationOptionsPropertName:
                        compilationOptions = ReadCompilationOptions(ref jsonReader);
                        break;
                    case DependencyContextStrings.TargetsPropertyName:
                        targets = ReadTargets(ref jsonReader);
                        break;
                    case DependencyContextStrings.LibrariesPropertyName:
                        libraryStubs = ReadLibraries(ref jsonReader);
                        break;
                    case DependencyContextStrings.RuntimesPropertyName:
                        runtimeFallbacks = ReadRuntimes(ref jsonReader);
                        break;
                    default:
                        jsonReader.Skip();
                        break;
                }
            }

            if (compilationOptions == null)
            {
                compilationOptions = CompilationOptions.Default;
            }

            Target runtimeTarget = SelectRuntimeTarget(targets, runtimeTargetName);
            runtimeTargetName = runtimeTarget?.Name;

            if (runtimeTargetName != null)
            {
                var seperatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeperator);
                if (seperatorIndex > -1 && seperatorIndex < runtimeTargetName.Length)
                {
                    runtime = runtimeTargetName.Substring(seperatorIndex + 1);
                    framework = runtimeTargetName.Substring(0, seperatorIndex);
                    isPortable = false;
                }
                else
                {
                    framework = runtimeTargetName;
                }
            }

            Target compileTarget = null;

            var ridlessTarget = targets.FirstOrDefault(t => !IsRuntimeTarget(t.Name));
            if (ridlessTarget != null)
            {
                compileTarget = ridlessTarget;
                if (runtimeTarget == null)
                {
                    runtimeTarget = compileTarget;
                    framework = ridlessTarget.Name;
                }
            }

            if (runtimeTarget == null)
            {
                throw new FormatException("No runtime target found");
            }

            // Holds document content, clear it before returning it.
            rentedBuffer.AsSpan().Clear();
            ArrayPool<byte>.Shared.Return(rentedBuffer);

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private Target SelectRuntimeTarget(List<Target> targets, string runtimeTargetName)
        {
            Target target;

            if (targets == null || targets.Count == 0)
            {
                throw new FormatException("Dependency file does not have 'targets' section");
            }

            if (!string.IsNullOrEmpty(runtimeTargetName))
            {
                target = targets.FirstOrDefault(t => t.Name == runtimeTargetName);
                if (target == null)
                {
                    throw new FormatException($"Target with name {runtimeTargetName} not found");
                }
            }
            else
            {
                target = targets.FirstOrDefault(t => IsRuntimeTarget(t.Name));
            }

            return target;
        }

        private bool IsRuntimeTarget(string name)
        {
            return name.Contains(DependencyContextStrings.VersionSeperator);
        }

        private void ReadRuntimeTarget(ref Utf8JsonReader reader, out string runtimeTargetName, out string runtimeSignature)
        {
            runtimeTargetName = null;
            runtimeSignature = null;

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
            {
                switch (propertyName)
                {
                    case DependencyContextStrings.RuntimeTargetNamePropertyName:
                        runtimeTargetName = propertyValue;
                        break;
                    case DependencyContextStrings.RuntimeTargetSignaturePropertyName:
                        runtimeSignature = propertyValue;
                        break;
                }
            }

            reader.CheckEndObject();
        }

        private CompilationOptions ReadCompilationOptions(ref Utf8JsonReader reader)
        {
            IEnumerable<string> defines = null;
            string languageVersion = null;
            string platform = null;
            bool? allowUnsafe = null;
            bool? warningsAsErrors = null;
            bool? optimize = null;
            string keyFile = null;
            bool? delaySign = null;
            bool? publicSign = null;
            string debugType = null;
            bool? emitEntryPoint = null;
            bool? generateXmlDocumentation = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.DefinesPropertyName:
                        defines = reader.ReadStringArray();
                        break;
                    case DependencyContextStrings.LanguageVersionPropertyName:
                        languageVersion = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.PlatformPropertyName:
                        platform = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.AllowUnsafePropertyName:
                        allowUnsafe = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.WarningsAsErrorsPropertyName:
                        warningsAsErrors = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.OptimizePropertyName:
                        optimize = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.KeyFilePropertyName:
                        keyFile = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.DelaySignPropertyName:
                        delaySign = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.PublicSignPropertyName:
                        publicSign = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.DebugTypePropertyName:
                        debugType = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.EmitEntryPointPropertyName:
                        emitEntryPoint = reader.ReadAsBoolean();
                        break;
                    case DependencyContextStrings.GenerateXmlDocumentationPropertyName:
                        generateXmlDocumentation = reader.ReadAsBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new CompilationOptions(
                defines ?? Enumerable.Empty<string>(),
                languageVersion,
                platform,
                allowUnsafe,
                warningsAsErrors,
                optimize,
                keyFile,
                delaySign,
                publicSign,
                debugType,
                emitEntryPoint,
                generateXmlDocumentation);
        }

        private List<Target> ReadTargets(ref Utf8JsonReader reader)
        {
            reader.ReadStartObject();

            var targets = new List<Target>();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                targets.Add(ReadTarget(ref reader, reader.GetStringValue()));
            }

            reader.CheckEndObject();

            return targets;
        }

        private Target ReadTarget(ref Utf8JsonReader reader, string targetName)
        {
            reader.ReadStartObject();

            var libraries = new List<TargetLibrary>();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                libraries.Add(ReadTargetLibrary(ref reader, reader.GetStringValue()));
            }

            reader.CheckEndObject();

            return new Target()
            {
                Name = targetName,
                Libraries = libraries
            };
        }

        private TargetLibrary ReadTargetLibrary(ref Utf8JsonReader reader, string targetLibraryName)
        {
            IEnumerable<Dependency> dependencies = null;
            List<RuntimeFile> runtimes = null;
            List<RuntimeFile> natives = null;
            List<string> compilations = null;
            List<RuntimeTargetEntryStub> runtimeTargets = null;
            List<ResourceAssembly> resources = null;
            bool? compileOnly = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.DependenciesPropertyName:
                        dependencies = ReadTargetLibraryDependencies(ref reader);
                        break;
                    case DependencyContextStrings.RuntimeAssembliesKey:
                        runtimes = ReadRuntimeFiles(ref reader);
                        break;
                    case DependencyContextStrings.NativeLibrariesKey:
                        natives = ReadRuntimeFiles(ref reader);
                        break;
                    case DependencyContextStrings.CompileTimeAssembliesKey:
                        compilations = ReadPropertyNames(ref reader);
                        break;
                    case DependencyContextStrings.RuntimeTargetsPropertyName:
                        runtimeTargets = ReadTargetLibraryRuntimeTargets(ref reader);
                        break;
                    case DependencyContextStrings.ResourceAssembliesPropertyName:
                        resources = ReadTargetLibraryResources(ref reader);
                        break;
                    case DependencyContextStrings.CompilationOnlyPropertyName:
                        compileOnly = reader.ReadAsBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new TargetLibrary()
            {
                Name = targetLibraryName,
                Dependencies = dependencies ?? Enumerable.Empty<Dependency>(),
                Runtimes = runtimes,
                Natives = natives,
                Compilations = compilations,
                RuntimeTargets = runtimeTargets,
                Resources = resources,
                CompileOnly = compileOnly
            };
        }

        private IEnumerable<Dependency> ReadTargetLibraryDependencies(ref Utf8JsonReader reader)
        {
            var dependencies = new List<Dependency>();

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string name, out string version))
            {
                dependencies.Add(new Dependency(Pool(name), Pool(version)));
            }

            reader.CheckEndObject();

            return dependencies;
        }

        private List<string> ReadPropertyNames(ref Utf8JsonReader reader)
        {
            var runtimes = new List<string>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string libraryName = reader.GetStringValue();
                reader.Skip();

                runtimes.Add(libraryName);
            }

            reader.CheckEndObject();

            return runtimes;
        }

        private List<RuntimeFile> ReadRuntimeFiles(ref Utf8JsonReader reader)
        {
            var runtimeFiles = new List<RuntimeFile>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string assemblyVersion = null;
                string fileVersion = null;

                string path = reader.GetStringValue();

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    switch (propertyName)
                    {
                        case DependencyContextStrings.AssemblyVersionPropertyName:
                            assemblyVersion = propertyValue;
                            break;
                        case DependencyContextStrings.FileVersionPropertyName:
                            fileVersion = propertyValue;
                            break;
                    }
                }

                reader.CheckEndObject();

                runtimeFiles.Add(new RuntimeFile(path, assemblyVersion, fileVersion));
            }

            reader.CheckEndObject();

            return runtimeFiles;
        }

        private List<RuntimeTargetEntryStub> ReadTargetLibraryRuntimeTargets(ref Utf8JsonReader reader)
        {
            var runtimeTargets = new List<RuntimeTargetEntryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var runtimeTarget = new RuntimeTargetEntryStub
                {
                    Path = reader.GetStringValue()
                };

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    switch (propertyName)
                    {
                        case DependencyContextStrings.RidPropertyName:
                            runtimeTarget.Rid = Pool(propertyValue);
                            break;
                        case DependencyContextStrings.AssetTypePropertyName:
                            runtimeTarget.Type = Pool(propertyValue);
                            break;
                        case DependencyContextStrings.AssemblyVersionPropertyName:
                            runtimeTarget.AssemblyVersion = propertyValue;
                            break;
                        case DependencyContextStrings.FileVersionPropertyName:
                            runtimeTarget.FileVersion = propertyValue;
                            break;
                    }
                }

                reader.CheckEndObject();

                runtimeTargets.Add(runtimeTarget);
            }

            reader.CheckEndObject();

            return runtimeTargets;
        }

        private List<ResourceAssembly> ReadTargetLibraryResources(ref Utf8JsonReader reader)
        {
            var resources = new List<ResourceAssembly>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string path = reader.GetStringValue();
                string locale = null;

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    if (propertyName == DependencyContextStrings.LocalePropertyName)
                    {
                        locale = propertyValue;
                    }
                }

                reader.CheckEndObject();

                if (locale != null)
                {
                    resources.Add(new ResourceAssembly(path, Pool(locale)));
                }
            }

            reader.CheckEndObject();

            return resources;
        }

        private Dictionary<string, LibraryStub> ReadLibraries(ref Utf8JsonReader reader)
        {
            var libraries = new Dictionary<string, LibraryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string libraryName = reader.GetStringValue();

                libraries.Add(Pool(libraryName), ReadOneLibrary(ref reader));
            }

            reader.CheckEndObject();

            return libraries;
        }

        private LibraryStub ReadOneLibrary(ref Utf8JsonReader reader)
        {
            string hash = null;
            string type = null;
            bool serviceable = false;
            string path = null;
            string hashPath = null;
            string runtimeStoreManifestName = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.Sha512PropertyName:
                        hash = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.TypePropertyName:
                        type = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.ServiceablePropertyName:
                        serviceable = reader.ReadAsNullableBoolean(defaultValue: false);
                        break;
                    case DependencyContextStrings.PathPropertyName:
                        path = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.HashPathPropertyName:
                        hashPath = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.RuntimeStoreManifestPropertyName:
                        runtimeStoreManifestName = reader.ReadAsString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new LibraryStub()
            {
                Hash = hash,
                Type = Pool(type),
                Serviceable = serviceable,
                Path = path,
                HashPath = hashPath,
                RuntimeStoreManifestName = runtimeStoreManifestName
            };
        }

        private List<RuntimeFallbacks> ReadRuntimes(ref Utf8JsonReader reader)
        {
            var runtimeFallbacks = new List<RuntimeFallbacks>();

            reader.ReadStartObject();

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string runtime = reader.GetStringValue();
                var fallbacks = reader.ReadStringArray();

                runtimeFallbacks.Add(new RuntimeFallbacks(runtime, fallbacks));
            }

            reader.CheckEndObject();

            return runtimeFallbacks;
        }

        private IEnumerable<Library> CreateLibraries(IEnumerable<TargetLibrary> libraries, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            if (libraries == null)
            {
                return Enumerable.Empty<Library>();
            }
            return libraries
                .Select(property => CreateLibrary(property, runtime, libraryStubs))
                .Where(library => library != null);
        }

        private Library CreateLibrary(TargetLibrary targetLibrary, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            var nameWithVersion = targetLibrary.Name;

            if (libraryStubs == null || !libraryStubs.TryGetValue(nameWithVersion, out LibraryStub stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var seperatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeperator);

            var name = Pool(nameWithVersion.Substring(0, seperatorPosition));
            var version = Pool(nameWithVersion.Substring(seperatorPosition + 1));

            if (runtime)
            {
                // Runtime section of this library was trimmed by type:platform
                var isCompilationOnly = targetLibrary.CompileOnly;
                if (isCompilationOnly == true)
                {
                    return null;
                }

                var runtimeAssemblyGroups = new List<RuntimeAssetGroup>();
                var nativeLibraryGroups = new List<RuntimeAssetGroup>();
                if (targetLibrary.RuntimeTargets != null)
                {
                    foreach (var ridGroup in targetLibrary.RuntimeTargets.GroupBy(e => e.Rid))
                    {
                        var groupRuntimeAssemblies = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.RuntimeAssetType)
                            .Select(e => new RuntimeFile(e.Path, e.AssemblyVersion, e.FileVersion))
                            .ToArray();

                        if (groupRuntimeAssemblies.Any())
                        {
                            runtimeAssemblyGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupRuntimeAssemblies.Where(a => Path.GetFileName(a.Path) != "_._")));
                        }

                        var groupNativeLibraries = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.NativeAssetType)
                            .Select(e => new RuntimeFile(e.Path, e.AssemblyVersion, e.FileVersion))
                            .ToArray();

                        if (groupNativeLibraries.Any())
                        {
                            nativeLibraryGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupNativeLibraries.Where(a => Path.GetFileName(a.Path) != "_._")));
                        }
                    }
                }

                if (targetLibrary.Runtimes != null && targetLibrary.Runtimes.Count > 0)
                {
                    runtimeAssemblyGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Runtimes));
                }

                if (targetLibrary.Natives != null && targetLibrary.Natives.Count > 0)
                {
                    nativeLibraryGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Natives));
                }

                return new RuntimeLibrary(
                    type: stub.Type,
                    name: name,
                    version: version,
                    hash: stub.Hash,
                    runtimeAssemblyGroups: runtimeAssemblyGroups,
                    nativeLibraryGroups: nativeLibraryGroups,
                    resourceAssemblies: targetLibrary.Resources ?? Enumerable.Empty<ResourceAssembly>(),
                    dependencies: targetLibrary.Dependencies,
                    serviceable: stub.Serviceable,
                    path: stub.Path,
                    hashPath: stub.HashPath,
                    runtimeStoreManifestName: stub.RuntimeStoreManifestName);
            }
            else
            {
                var assemblies = targetLibrary.Compilations ?? Enumerable.Empty<string>();
                return new CompilationLibrary(
                    stub.Type,
                    name,
                    version,
                    stub.Hash,
                    assemblies,
                    targetLibrary.Dependencies,
                    stub.Serviceable,
                    stub.Path,
                    stub.HashPath);
            }
        }

        private string Pool(string s)
        {
            if (s == null)
            {
                return null;
            }

            if (!_stringPool.TryGetValue(s, out string result))
            {
                _stringPool[s] = s;
                result = s;
            }
            return result;
        }

        private class Target
        {
            public string Name;

            public IEnumerable<TargetLibrary> Libraries;
        }

        private struct TargetLibrary
        {
            public string Name;

            public IEnumerable<Dependency> Dependencies;

            public List<RuntimeFile> Runtimes;

            public List<RuntimeFile> Natives;

            public List<string> Compilations;

            public List<RuntimeTargetEntryStub> RuntimeTargets;

            public List<ResourceAssembly> Resources;

            public bool? CompileOnly;
        }

        private struct RuntimeTargetEntryStub
        {
            public string Type;

            public string Path;

            public string Rid;

            public string AssemblyVersion;

            public string FileVersion;
        }

        private struct LibraryStub
        {
            public string Hash;

            public string Type;

            public bool Serviceable;

            public string Path;

            public string HashPath;

            public string RuntimeStoreManifestName;
        }
    }
}
