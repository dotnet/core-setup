﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class BundledAppWithSubDirs : IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void Bundle_And_Run_App_With_Subdirs_Succeeds()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var hostName = Path.GetFileName(fixture.TestProject.AppExe);

            // Bundle to a single-file
            // This step should be removed in favor of publishing with /p:PublishSingleFile=true
            // once associated changes in SDK repo are checked in.
            string singleFileDir = Path.Combine(fixture.TestProject.ProjectDirectory, "oneExe");
            Directory.CreateDirectory(singleFileDir);
            string bundleDll = Path.Combine(sharedTestState.RepoDirectories.Artifacts,
                                            "Microsoft.NET.Build.Bundle",
                                            "netcoreapp2.0",
                                            "Microsoft.NET.Build.Bundle.dll");
            string[] bundleArgs = { "--source", fixture.TestProject.OutputDirectory,
                                    "--apphost", hostName,
                                    "--output", singleFileDir };

            fixture.SdkDotnet.Exec(bundleDll, bundleArgs)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            // Run the bundled app
            string singleFile = Path.Combine(singleFileDir, hostName);
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("StandaloneAppWithSubDirs", RepoDirectories);
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
