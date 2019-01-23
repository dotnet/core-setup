// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextJsonReader : IDependencyContextReader
    {
        private readonly IDictionary<string, string> _stringPool = new Dictionary<string, string>();

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
            return name.Contains(DependencyContextStrings.VersionSeparator);
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

            var separatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeparator);

            var name = Pool(nameWithVersion.Substring(0, separatorPosition));
            var version = Pool(nameWithVersion.Substring(separatorPosition + 1));

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
