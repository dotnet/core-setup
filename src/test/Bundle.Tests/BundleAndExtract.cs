// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.BundleTests.BundleExtract
{
    public class BundleAndExtract : IClassFixture<BundleAndExtract.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleAndExtract(BundleAndExtract.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Test()
        {
            var fixture = sharedTestState.TestFixture
                .Copy();

            var dotnet = fixture.SdkDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Run the App normally
            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            // Bundle to a single-file
            string singleFileDir = Path.Combine(fixture.TestProject.OutputDirectory, "oneExe");
            Directory.CreateDirectory(singleFileDir);

            string hostName = Path.GetFileName(fixture.TestProject.AppExe);
            string bundleDll = Path.Combine(sharedTestState.RepoDirectories.Artifacts,
                                            "bundle",
                                            "netcoreapp2.0",
                                            "bundle.dll");
            string[] bundleArgs = { "-d", fixture.TestProject.OutputDirectory,
                                    "-a", hostName,
                                    "-o", singleFileDir };

            dotnet.Exec(bundleDll, bundleArgs)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            // Extract the contents
            string singleFile = Path.Combine(singleFileDir, hostName);
            string[] extractArgs = { "-e", singleFile,
                                    "-o", singleFileDir };

            dotnet.Exec(bundleDll, extractArgs)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            // Run the extracted app
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid);
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
