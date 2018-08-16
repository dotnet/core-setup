// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutNativeHostApis
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredPortableTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutNativeHostApis()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_hostfxr_get_native_search_directories_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture.Copy();
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

        private class SdkResolutionFixture
        {
            private readonly TestProjectFixture _fixture;

            public DotNetCli Dotnet => _fixture.BuiltDotnet;
            public string AppDll => _fixture.TestProject.AppDll;
            public string ExeDir => Path.Combine(_fixture.TestProject.ProjectDirectory, "ed");
            public string ProgramFiles => Path.Combine(ExeDir, "pf");
            public string WorkingDir => Path.Combine(_fixture.TestProject.ProjectDirectory, "wd");
            public string GlobalSdkDir => Path.Combine(ProgramFiles, "dotnet", "sdk");
            public string LocalSdkDir => Path.Combine(ExeDir, "sdk");
            public string GlobalJson => Path.Combine(WorkingDir, "global.json");
            public string[] GlobalSdks = new[] { "4.5.6", "1.2.3", "2.3.4-preview" };
            public string[] LocalSdks = new[] { "0.1.2", "5.6.7-preview", "1.2.3" };

            public SdkResolutionFixture()
            {
                _fixture =  PreviouslyPublishedAndRestoredPortableTestProjectFixture.Copy();
                SetupPInvokeToHostfxrOnNonWindows(_fixture);

                Directory.CreateDirectory(WorkingDir);

                // start with an empty global.json, it will be ignored, but prevent one lying on disk 
                // on a given machine from impacting the test.
                File.WriteAllText(GlobalJson, "{}");

                foreach (string sdk in GlobalSdks)
                {
                    Directory.CreateDirectory(Path.Combine(GlobalSdkDir, sdk));
                }

                foreach (string sdk in LocalSdks)
                {
                    Directory.CreateDirectory(Path.Combine(LocalSdkDir, sdk));
                }
            } 
        }

        [Fact]
        public void Hostfxr_get_available_sdks_with_multilevel_lookup()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                // multilevel lookup is not supported on non-Windows
                return;
            }
            
            var f = new SdkResolutionFixture();

            // With multi-level lookup (windows onnly): get local and global sdks sorted by ascending version,
            // with global sdk coming before local sdk when versions are equal
            string expectedList = string.Join(';', new[]
            {
                Path.Combine(f.LocalSdkDir, "0.1.2"),
                Path.Combine(f.GlobalSdkDir, "1.2.3"),
                Path.Combine(f.LocalSdkDir, "1.2.3"),
                Path.Combine(f.GlobalSdkDir, "2.3.4-preview"),
                Path.Combine(f.GlobalSdkDir, "4.5.6"),
                Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_available_sdks", f.ExeDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
        }

        [Fact]
        public void Hostfxr_get_available_sdks_without_multilevel_lookup()
        {
            // Without multi-level lookup: get only sdks sorted by ascending version

            var f = new SdkResolutionFixture();

            string expectedList = string.Join(';', new[]
            {
                 Path.Combine(f.LocalSdkDir, "0.1.2"),
                 Path.Combine(f.LocalSdkDir, "1.2.3"),
                 Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_available_sdks", f.ExeDir })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_or_flags()
        {
            // with no global.json and no flags, pick latest SDK

            var f = new SdkResolutionFixture();

            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "0" })
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

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_and_disallowing_previews()
        {
            // Without global.json and disallowing previews, pick latest non-preview

            var f = new SdkResolutionFixture();

            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "1.2.3"))
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "disallow_prerelease" })
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

        [Fact]
        public void Hostfxr_resolve_sdk2_with_global_json_and_disallowing_previews()
        {
            // With global.json specifying a preview, roll forward to preview 
            // since flag has no impact if global.json specifies a preview.
            // Also check that global.json that impacted resolution is reported.

            var f = new SdkResolutionFixture();

            File.WriteAllText(f.GlobalJson, "{ \"sdk\": { \"version\": \"5.6.6-preview\" } }");
            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
                ("global_json_path", f.GlobalJson),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "disallow_prerelease" })
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

        private static void SetupPInvokeToHostfxrOnNonWindows(TestProjectFixture fixture)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On non-Windows, we can't just P/Invoke to already loaded hostfxr, so copy it next to the app dll.
                var hostfxr = Path.Combine(
                    fixture.BuiltDotnet.GreatestVersionHostFxrPath, 
                    $"{fixture.SharedLibraryPrefix}hostfxr{fixture.SharedLibraryExtension}");

                File.Copy(
                    hostfxr, 
                    Path.GetDirectoryName(fixture.TestProject.AppDll));
            }
        }
    }
}
