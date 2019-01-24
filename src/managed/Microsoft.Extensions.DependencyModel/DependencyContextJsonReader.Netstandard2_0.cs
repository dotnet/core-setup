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
    public partial class DependencyContextJsonReader : IDependencyContextReader
    {
        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ArraySegment<byte> drained = ReadToEnd(stream);
            try
            {
                return ReadCore(drained);
            }
            finally
            {
                // Holds document content, clear it before returning it.
                drained.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(drained.Array);
            }
        }

        private static bool IsTokenTypeProperty(JsonTokenType tokenType)
            => tokenType == JsonTokenType.PropertyName;

        private DependencyContext ReadCore(ReadOnlySpan<byte> jsonData)
        {
            var reader = new Utf8JsonReader(jsonData, isFinalBlock: true, state: default);

            reader.ReadStartObject();

            string runtime = string.Empty;
            string framework = string.Empty;
            bool isPortable = true;
            string runtimeTargetName = null;
            string runtimeSignature = null;

            CompilationOptions compilationOptions = null;
            List<Target> targets = null;
            Dictionary<string, LibraryStub> libraryStubs = null;
            List<RuntimeFallbacks> runtimeFallbacks = null;

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                switch (reader.GetString())
                {
                    case DependencyContextStrings.RuntimeTargetPropertyName:
                        ReadRuntimeTarget(ref reader, out runtimeTargetName, out runtimeSignature);
                        break;
                    case DependencyContextStrings.CompilationOptionsPropertName:
                        compilationOptions = ReadCompilationOptions(ref reader);
                        break;
                    case DependencyContextStrings.TargetsPropertyName:
                        targets = ReadTargets(ref reader);
                        break;
                    case DependencyContextStrings.LibrariesPropertyName:
                        libraryStubs = ReadLibraries(ref reader);
                        break;
                    case DependencyContextStrings.RuntimesPropertyName:
                        runtimeFallbacks = ReadRuntimes(ref reader);
                        break;
                    default:
                        reader.Skip();
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
                int separatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeparator);
                if (separatorIndex > -1 && separatorIndex < runtimeTargetName.Length)
                {
                    runtime = runtimeTargetName.Substring(separatorIndex + 1);
                    framework = runtimeTargetName.Substring(0, separatorIndex);
                    isPortable = false;
                }
                else
                {
                    framework = runtimeTargetName;
                }
            }

            Target compileTarget = null;

            Target ridlessTarget = targets.FirstOrDefault(t => !IsRuntimeTarget(t.Name));
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

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private static void ReadRuntimeTarget(ref Utf8JsonReader reader, out string runtimeTargetName, out string runtimeSignature)
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

        private static CompilationOptions ReadCompilationOptions(ref Utf8JsonReader reader)
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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                switch (reader.GetString())
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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                targets.Add(ReadTarget(ref reader, reader.GetString()));
            }

            reader.CheckEndObject();

            return targets;
        }

        private Target ReadTarget(ref Utf8JsonReader reader, string targetName)
        {
            reader.ReadStartObject();

            var libraries = new List<TargetLibrary>();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                libraries.Add(ReadTargetLibrary(ref reader, reader.GetString()));
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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                switch (reader.GetString())
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

        private static List<string> ReadPropertyNames(ref Utf8JsonReader reader)
        {
            var runtimes = new List<string>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string libraryName = reader.GetString();
                reader.Skip();

                runtimes.Add(libraryName);
            }

            reader.CheckEndObject();

            return runtimes;
        }

        private static List<RuntimeFile> ReadRuntimeFiles(ref Utf8JsonReader reader)
        {
            var runtimeFiles = new List<RuntimeFile>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string assemblyVersion = null;
                string fileVersion = null;

                string path = reader.GetString();

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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var runtimeTarget = new RuntimeTargetEntryStub
                {
                    Path = reader.GetString()
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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string path = reader.GetString();
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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string libraryName = reader.GetString();

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

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                switch (reader.GetString())
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

        private static List<RuntimeFallbacks> ReadRuntimes(ref Utf8JsonReader reader)
        {
            var runtimeFallbacks = new List<RuntimeFallbacks>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string runtime = reader.GetString();
                string[] fallbacks = reader.ReadStringArray();

                runtimeFallbacks.Add(new RuntimeFallbacks(runtime, fallbacks));
            }

            reader.CheckEndObject();

            return runtimeFallbacks;
        }

        // Borrowed from https://github.com/dotnet/corefx/blob/ef23e3317ca6e83f1e959ab265a8e59fb8a6dcd9/src/System.Text.Json/src/System/Text/Json/Document/JsonDocument.Parse.cs#L176-L225
        private static ArraySegment<byte> ReadToEnd(Stream stream)
        {
            int written = 0;
            byte[] rented = null;

            try
            {
                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    long expectedLength = Math.Max(0, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                do
                {
                    if (rented.Length == written)
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

        private const int UnseekableStreamInitialRentSize = 4096;
    }
}
