// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class DotNetBuilder
    {
        private readonly string _path;
        private readonly RepoDirectoriesProvider _repoDirectories;

        public DotNetBuilder(string basePath, string builtDotnet, string name)
        {
            _path = Path.Combine(basePath, name);
            Directory.CreateDirectory(_path);

            _repoDirectories = new RepoDirectoriesProvider(builtDotnet: _path);

            // Prepare the dotnet installation mock

            // ./dotnet.exe - used as a convenient way to load and invoke hostfxr. May change in the future to use test-specific executable
            var builtDotNetCli = new DotNetCli(builtDotnet);
            File.Copy(
                builtDotNetCli.DotnetExecutablePath,
                Path.Combine(_path, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("dotnet")),
                true);

            // ./host/fxr/<version>/hostfxr.dll - this is the component being tested
            SharedFramework.CopyDirectory(
                builtDotNetCli.GreatestVersionHostFxrPath,
                Path.Combine(_path, "host", "fxr", Path.GetFileName(builtDotNetCli.GreatestVersionHostFxrPath)));
        }

        public DotNetBuilder AddMicrosoftNETCoreAppFramework(string version)
        {
            // ./shared/Microsoft.NETCore.App/<version> - create a mock of the root framework
            string netCoreAppPath = Path.Combine(_path, "shared", "Microsoft.NETCore.App", version);
            Directory.CreateDirectory(netCoreAppPath);

            // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is a mock, will not actually load CoreCLR
            string mockHostPolicyFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
            File.Copy(
                Path.Combine(_repoDirectories.Artifacts, "corehost_test", mockHostPolicyFileName),
                Path.Combine(netCoreAppPath, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy")),
                true);

            return this;
        }

        public DotNetBuilder AddFramework(
            string name,
            string version,
            Action<RuntimeConfig> runtimeConfigCustomizer)
        {
            // ./shared/<name>/<version> - create a mock of effectively empty non-root framework
            string path = Path.Combine(_path, "shared", name, version);
            Directory.CreateDirectory(path);

            // ./shared/<name>/<version>/<name>.runtimeconfig.json - runtime config which can be customized
            RuntimeConfig runtimeConfig = new RuntimeConfig(Path.Combine(path, name + ".runtimeconfig.json"));
            runtimeConfigCustomizer(runtimeConfig);
            runtimeConfig.Save();

            return this;
        }

        public DotNetCli Build()
        {
            return new DotNetCli(_path);
        }
    }
}
