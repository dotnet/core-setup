// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Nethost : IClassFixture<Nethost.SharedTestState>
    {
        private const string GetHostFxrPath = "nethost_get_hostfxr_path";
        private const int CoreHostLibMissingFailure = unchecked((int)0x80008083);

        private readonly SharedTestState sharedState;

        public Nethost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void GetHostFxrPath_DotNetRootEnvironment(bool useAssemblyPath, bool isValid)
        {
            string dotNetRoot = isValid ? Path.Combine(sharedState.ValidInstallRoot, "dotnet") : sharedState.InvalidInstallRoot;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("DOTNET_ROOT", dotNetRoot)
                .Execute();

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(CoreHostLibMissingFailure)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed");
            }
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void GetHostFxrPath_GlobalInstallation(bool useAssemblyPath, bool isValid)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // We don't have a good way of hooking into how the product looks for global installations yet.
                return;
            }

            string programFilesOverride = isValid ? sharedState.ValidInstallRoot : sharedState.InvalidInstallRoot;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("TEST_OVERRIDE_PROGRAMFILES", programFilesOverride)
                .Execute();

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(CoreHostLibMissingFailure)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed");
            }
        }

        [Fact]
        public void GetHostFxrPath_WithAssemblyPath_AppLocalFxr()
        {
            string appLocalFxrDir = Path.Combine(sharedState.BaseDirectory, "appLocalFxr");
            Directory.CreateDirectory(appLocalFxrDir);
            string assemblyPath = Path.Combine(appLocalFxrDir, "AppLocalFxr.dll");
            string hostFxrPath = Path.Combine(appLocalFxrDir, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));
            File.WriteAllText(assemblyPath, string.Empty);
            File.WriteAllText(hostFxrPath, string.Empty);

            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {assemblyPath}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {hostFxrPath}".ToLower());
        }

        public class SharedTestState : IDisposable
        {
            public string BaseDirectory { get; }
            public string NativeHostPath { get; }

            public string HostFxrPath { get; }
            public string InvalidInstallRoot { get; }
            public string ValidInstallRoot { get; }

            public string TestAssemblyPath { get; }

            public SharedTestState()
            {
                BaseDirectory = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "nativeHosting"));
                Directory.CreateDirectory(BaseDirectory);

                string nativeHostName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("nativehost");
                NativeHostPath = Path.Combine(BaseDirectory, nativeHostName);

                // Copy over native host and nethost
                var repoDirectories = new RepoDirectoriesProvider();
                string nethostName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("nethost");
                File.Copy(Path.Combine(repoDirectories.CorehostPackages, nethostName), Path.Combine(BaseDirectory, nethostName));
                File.Copy(Path.Combine(repoDirectories.Artifacts, "corehost_test", nativeHostName), NativeHostPath);

                InvalidInstallRoot = Path.Combine(BaseDirectory, "invalid");
                Directory.CreateDirectory(InvalidInstallRoot);

                ValidInstallRoot = Path.Combine(BaseDirectory, "valid");
                HostFxrPath = CreateHostFxr(Path.Combine(ValidInstallRoot, "dotnet"));

                string appDir = Path.Combine(BaseDirectory, "app");
                Directory.CreateDirectory(appDir);
                string assemblyPath = Path.Combine(appDir, "App.dll");
                File.WriteAllText(assemblyPath, string.Empty);
                TestAssemblyPath = assemblyPath;
            }

            private string CreateHostFxr(string destinationDirectory)
            {
                string hostFxrName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
                string fxrRoot = Path.Combine(destinationDirectory, "host", "fxr");
                Directory.CreateDirectory(fxrRoot);

                string[] versions = new string[] { "1.1.0", "2.2.1", "2.3.0" };
                foreach (string version in versions)
                {
                    string versionDirectory = Path.Combine(fxrRoot, version);
                    Directory.CreateDirectory(versionDirectory);
                    File.WriteAllText(Path.Combine(versionDirectory, hostFxrName), string.Empty);
                }

                return Path.Combine(fxrRoot, "2.3.0", hostFxrName);
            }

            public void Dispose()
            {
                if (!TestArtifact.PreserveTestRuns() && Directory.Exists(BaseDirectory))
                {
                    Directory.Delete(BaseDirectory, true);
                }
            }
        }
    }
}
