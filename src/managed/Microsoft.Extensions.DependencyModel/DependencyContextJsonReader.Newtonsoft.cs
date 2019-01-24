// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

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

            using (var streamReader = new StreamReader(stream))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    return ReadCore(reader);
                }
            }
        }

        private static bool IsTokenTypeProperty(JsonToken tokenType)
            => tokenType == JsonToken.PropertyName;

        private DependencyContext ReadCore(JsonTextReader reader)
        {
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
                switch ((string)reader.Value)
                {
                    case DependencyContextStrings.RuntimeTargetPropertyName:
                        ReadRuntimeTarget(reader, out runtimeTargetName, out runtimeSignature);
                        break;
                    case DependencyContextStrings.CompilationOptionsPropertName:
                        compilationOptions = ReadCompilationOptions(reader);
                        break;
                    case DependencyContextStrings.TargetsPropertyName:
                        targets = ReadTargets(reader);
                        break;
                    case DependencyContextStrings.LibrariesPropertyName:
                        libraryStubs = ReadLibraries(reader);
                        break;
                    case DependencyContextStrings.RuntimesPropertyName:
                        runtimeFallbacks = ReadRuntimes(reader);
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
                var separatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeparator);
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

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private static void ReadRuntimeTarget(JsonTextReader reader, out string runtimeTargetName, out string runtimeSignature)
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

        private static CompilationOptions ReadCompilationOptions(JsonTextReader reader)
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
                switch ((string)reader.Value)
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

        private List<Target> ReadTargets(JsonTextReader reader)
        {
            reader.ReadStartObject();

            var targets = new List<Target>();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                targets.Add(ReadTarget(reader, (string)reader.Value));
            }

            reader.CheckEndObject();

            return targets;
        }

        private Target ReadTarget(JsonTextReader reader, string targetName)
        {
            reader.ReadStartObject();

            var libraries = new List<TargetLibrary>();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                libraries.Add(ReadTargetLibrary(reader, (string)reader.Value));
            }

            reader.CheckEndObject();

            return new Target()
            {
                Name = targetName,
                Libraries = libraries
            };
        }

        private TargetLibrary ReadTargetLibrary(JsonTextReader reader, string targetLibraryName)
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
                switch ((string)reader.Value)
                {
                    case DependencyContextStrings.DependenciesPropertyName:
                        dependencies = ReadTargetLibraryDependencies(reader);
                        break;
                    case DependencyContextStrings.RuntimeAssembliesKey:
                        runtimes = ReadRuntimeFiles(reader);
                        break;
                    case DependencyContextStrings.NativeLibrariesKey:
                        natives = ReadRuntimeFiles(reader);
                        break;
                    case DependencyContextStrings.CompileTimeAssembliesKey:
                        compilations = ReadPropertyNames(reader);
                        break;
                    case DependencyContextStrings.RuntimeTargetsPropertyName:
                        runtimeTargets = ReadTargetLibraryRuntimeTargets(reader);
                        break;
                    case DependencyContextStrings.ResourceAssembliesPropertyName:
                        resources = ReadTargetLibraryResources(reader);
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

        private IEnumerable<Dependency> ReadTargetLibraryDependencies(JsonTextReader reader)
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

        private static List<string> ReadPropertyNames(JsonTextReader reader)
        {
            var runtimes = new List<string>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var libraryName = (string)reader.Value;
                reader.Skip();

                runtimes.Add(libraryName);
            }

            reader.CheckEndObject();

            return runtimes;
        }

        private static List<RuntimeFile> ReadRuntimeFiles(JsonTextReader reader)
        {
            var runtimeFiles = new List<RuntimeFile>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                string assemblyVersion = null;
                string fileVersion = null;

                var path = (string)reader.Value;

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

        private List<RuntimeTargetEntryStub> ReadTargetLibraryRuntimeTargets(JsonTextReader reader)
        {
            var runtimeTargets = new List<RuntimeTargetEntryStub>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var runtimeTarget = new RuntimeTargetEntryStub
                {
                    Path = (string)reader.Value
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

        private List<ResourceAssembly> ReadTargetLibraryResources(JsonTextReader reader)
        {
            var resources = new List<ResourceAssembly>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var path = (string)reader.Value;
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

        private Dictionary<string, LibraryStub> ReadLibraries(JsonTextReader reader)
        {
            var libraries = new Dictionary<string, LibraryStub>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var libraryName = (string)reader.Value;

                libraries.Add(Pool(libraryName), ReadOneLibrary(reader));
            }

            reader.CheckEndObject();

            return libraries;
        }

        private LibraryStub ReadOneLibrary(JsonTextReader reader)
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
                switch ((string)reader.Value)
                {
                    case DependencyContextStrings.Sha512PropertyName:
                        hash = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.TypePropertyName:
                        type = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.ServiceablePropertyName:
                        serviceable = reader.ReadAsBoolean().GetValueOrDefault(false);
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

        private static List<RuntimeFallbacks> ReadRuntimes(JsonTextReader reader)
        {
            var runtimeFallbacks = new List<RuntimeFallbacks>();

            reader.ReadStartObject();

            while (reader.Read() && IsTokenTypeProperty(reader.TokenType))
            {
                var runtime = (string)reader.Value;
                var fallbacks = reader.ReadStringArray();

                runtimeFallbacks.Add(new RuntimeFallbacks(runtime, fallbacks));
            }

            reader.CheckEndObject();

            return runtimeFallbacks;
        }
    }
}
