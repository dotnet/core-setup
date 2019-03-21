// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Nethost : IClassFixture<Nethost.SharedTestState>
    {
        private const string GetHostFxrPath = "nethost_get_hostfxr_path";

        private readonly SharedTestState sharedState;
        private readonly DotNetCli dotNetCli;

        public Nethost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
            dotNetCli = new DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
        }

        [Fact]
        public void GetHostFxrPath_NoAssemblyPath_NoFxr()
        {
            Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail();
        }

        [Fact]
        public void GetHostFxrPath_NoAssemblyPath_FxrSubdirectory()
        {
            string copiedHostPath = Path.Combine(Path.GetDirectoryName(sharedState.NativeHostPath), "host");
            SharedFramework.CopyDirectory(Path.Combine(dotNetCli.BinPath, "host"), copiedHostPath);
            CommandResult result = Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            Directory.Delete(copiedHostPath, true);

            string expectedFxrPath = Path.Combine(
                copiedHostPath,
                "fxr",
                Path.GetFileName(dotNetCli.GreatestVersionHostFxrPath),
                RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));
            result.Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {expectedFxrPath}".ToLower());
        }

        [Fact]
        public void GetHostFxrPath_WithAssemblyPath_AppLocalFxr()
        {
            TestProjectFixture fixture = sharedState.StandaloneAppFixture;
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {fixture.TestProject.AppDll}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {fixture.TestProject.HostFxrDll}".ToLower());
        }

        [Fact]
        public void GetHostFxrPath_WithAssemblyPath_DotNetRootEnvironment()
        {
            string expectedFxrPath = Path.Combine(
                dotNetCli.GreatestVersionHostFxrPath,
                RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {sharedState.NativeHostPath}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("DOTNET_ROOT", dotNetCli.BinPath)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {expectedFxrPath}".ToLower());
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; }
            public TestProjectFixture StandaloneAppFixture { get; }
            public string NativeHostPath { get; }

            private readonly string baseDir;

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                baseDir = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "nativeHosting"));
                Directory.CreateDirectory(baseDir);

                string nativeHostName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("nativehost");
                NativeHostPath = Path.Combine(baseDir, nativeHostName);

                // Copy over native host and nethost
                string nethostName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("nethost");
                File.Copy(Path.Combine(RepoDirectories.CorehostPackages, nethostName), Path.Combine(baseDir, nethostName));
                File.Copy(Path.Combine(RepoDirectories.Artifacts, "corehost_test", nativeHostName), NativeHostPath);

                var standaloneAppFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                StandaloneAppFixture = standaloneAppFixture
                    .EnsureRestoredForRid(standaloneAppFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: standaloneAppFixture.CurrentRid);
            }

            public void Dispose()
            {
                StandaloneAppFixture.Dispose();
                if (!TestArtifact.PreserveTestRuns() && Directory.Exists(baseDir))
                {
                    Directory.Delete(baseDir, true);
                }
            }
        }
    }
}
