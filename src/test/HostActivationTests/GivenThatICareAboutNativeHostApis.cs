// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutNativeHostApis : IClassFixture<GivenThatICareAboutNativeHostApis.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutNativeHostApis(GivenThatICareAboutNativeHostApis.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_hostfxr_get_native_search_directories_Succeeds()
        {
            // https://github.com/dotnet/core-setup/issues/4344
            // Currently the native API is used only on Windows, although it has been manually tested on Unix.
            // Limit OS here to avoid issues with DllImport not being able to find the shared library.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableTestProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var dotnetLocation = Path.Combine(dotnet.BinPath, $"dotnet{fixture.ExeExtension}");
            string[] args =
            {
                "hostfxr_get_native_search_directories",
                dotnetLocation,
                appDll
            };

            dotnet.Exec(appDll, args)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories:Success")
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories buffer:[" + dotnet.GreatestVersionSharedFxPath);
        }
        
         [Fact]
         public void Hostfxr_resolve_sdk2_and_hostfxr_get_available_sdks_work()
        {
            // https://github.com/dotnet/core-setup/issues/4344
            // Limit OS here to avoid issues with DllImport not being able to find the shared library.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableTestProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string[] globalSdks = new[] { "4.5.6", "1.2.3", "2.3.4-preview" };
            string[] localSdks = new[] { "0.1.2", "5.6.7-preview", "1.2.3" };

            var exeDir = Path.Combine(fixture.TestProject.ProjectDirectory, "ed");
            var programFiles = Path.Combine(exeDir, "pf");
            var workingDir= Path.Combine(fixture.TestProject.ProjectDirectory, "wd");
            var globalSdkDir = Path.Combine(programFiles, "dotnet", "sdk");
            var localSdkDir = Path.Combine(exeDir, "sdk");

            // start with an empty global.json, it will be ignored, but prevent one lying on disk 
            // on a given machine from impacting the test.
            var globalJson = Path.Combine(workingDir, "global.json");
            Directory.CreateDirectory(workingDir);
            File.WriteAllText(globalJson, "{}");

            foreach (string sdk in globalSdks)
            {
                Directory.CreateDirectory(Path.Combine(globalSdkDir, sdk));
            }

            foreach (string sdk in localSdks)
            {
                Directory.CreateDirectory(Path.Combine(localSdkDir, sdk));
            }

            var dotnetLocation = Path.Combine(dotnet.BinPath, $"dotnet{fixture.ExeExtension}");

            // With multi-level lookup: get local and global sdks sorted by ascending version,
            // with global sdk coming before local sdk when versions are equal
            var expectedList = string.Join(';', new[]
            {
                 Path.Combine(localSdkDir, "0.1.2"),
                 Path.Combine(globalSdkDir, "1.2.3"),
                 Path.Combine(localSdkDir, "1.2.3"),
                 Path.Combine(globalSdkDir, "2.3.4-preview"), 
                 Path.Combine(globalSdkDir, "4.5.6"),
                 Path.Combine(localSdkDir, "5.6.7-preview"),
            });

            dotnet.Exec(appDll, new[] { "hostfxr_get_available_sdks", exeDir } )
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", programFiles)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");

            // Without multi-level lookup: get only sdks sorted by ascending version
            expectedList = string.Join(';', new[]
            {
                 Path.Combine(localSdkDir, "0.1.2"),
                 Path.Combine(localSdkDir, "1.2.3"),
                 Path.Combine(localSdkDir, "5.6.7-preview"),
            });

            dotnet.Exec(appDll, new[] { "hostfxr_get_available_sdks", exeDir })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");

            // with no global.json and no flags, pick latest SDK
            var expectedData = string.Join(';', new[] 
            {
                ("resolved_sdk_dir", Path.Combine(localSdkDir, "5.6.7-preview")),
            });

            dotnet.Exec(appDll, new[] { "hostfxr_resolve_sdk2", exeDir, workingDir, "0" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");

            // without global.json and disallowing previews, pick latest non-preview
            expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(globalSdkDir, "4.5.6"))
            });

            dotnet.Exec(appDll, new[] { "hostfxr_resolve_sdk2", exeDir, workingDir, "disallow_prerelease" })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", programFiles)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");

            // Without global.json and disallowing previews, roll forward to preview 
            // since flag has no impact if global.json specifies a preview.
            // Also check that global.json that impacted resolution is reported.
            File.WriteAllText(globalJson, "{ \"sdk\": { \"version\": \"5.6.6-preview\" } }");
            expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(localSdkDir, "5.6.7-preview")),
                ("global_json_path", globalJson),
            });

            dotnet.Exec(appDll, new[] { "hostfxr_resolve_sdk2", exeDir, workingDir, "disallow_prerelease" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableTestProjectFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PreviouslyPublishedAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                PreviouslyBuiltAndRestoredPortableTestProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredPortableTestProjectFixture.Dispose();
            }
        }
    }
}
