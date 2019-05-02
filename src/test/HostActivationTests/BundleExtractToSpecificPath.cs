// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class BundleExtractToSpecificPath : IClassFixture<BundleExtractToSpecificPath.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleExtractToSpecificPath(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private static string GetHostName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppExe);
        }

        private static string GetPublishPath(TestProjectFixture fixture)
        {
            return Path.Combine(fixture.TestProject.ProjectDirectory, "publish");
        }

        private static DirectoryInfo GetBundleDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(Path.Combine(fixture.TestProject.ProjectDirectory, "bundle"));
        }

        private static DirectoryInfo GetExtractDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(Path.Combine(fixture.TestProject.ProjectDirectory, "extract"));
        }

        [Fact]
        private void Bundle_Extraction_To_Specific_Path_Succeeds()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var hostName = GetHostName(fixture);
            var appName = Path.GetFileNameWithoutExtension(hostName);
            string publishPath = GetPublishPath(fixture);

            // Publish the bundle
            var bundleDir = GetBundleDir(fixture);
            var bundler = new Microsoft.NET.HostModel.Bundle.Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(publishPath);

            // Compute bundled files
            var bundledFiles = new List<string>(bundler.BundleManifest.Files.Count);
            bundler.BundleManifest.Files.ForEach(file => bundledFiles.Add(file.RelativePath));

            // Verify expected files in the bundle directory
            bundleDir.Should().HaveFile(hostName);
            bundleDir.Should().NotHaveFiles(bundledFiles);

            // Create a directory for extraction.
            var extractBaseDir = GetExtractDir(fixture);
            extractBaseDir.Should().NotHaveDirectory(appName);

            // Run the bunded app for the first time, and extract files to 
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Environment.SetEnvironmentVariable("DOTNET_BUNDLE_EXTRACT_BASE_DIR", extractBaseDir.FullName);
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            string extractPath = Path.Combine(extractBaseDir.FullName, appName, bundler.BundleManifest.BundleID);
            var extractDir = new DirectoryInfo(extractPath);
            extractDir.Should().OnlyHaveFiles(bundledFiles);
            extractDir.Should().NotHaveFile(hostName);
        }

        [Fact]
        private void Bundle_extraction_is_reused()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var hostName = GetHostName(fixture);
            var appName = Path.GetFileNameWithoutExtension(hostName);
            string publishPath = GetPublishPath(fixture);

            // Publish the bundle
            var bundleDir = GetBundleDir(fixture);
            var bundler = new Microsoft.NET.HostModel.Bundle.Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(publishPath);

            // Create a directory for extraction.
            var extractBaseDir = GetExtractDir(fixture);

            // Run the bunded app for the first time, and extract files to 
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Environment.SetEnvironmentVariable("DOTNET_BUNDLE_EXTRACT_BASE_DIR", extractBaseDir.FullName);
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            string extractPath = Path.Combine(extractBaseDir.FullName, appName, bundler.BundleManifest.BundleID);
            var extractDir = new DirectoryInfo(extractPath);

            extractDir.Refresh();
            DateTime firstWriteTime = extractDir.LastWriteTimeUtc;

            while (DateTime.Now == firstWriteTime)
            {
                Thread.Sleep(1);
            }

            // Run the bundled app again (reuse extracted files)
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            extractDir.Should().NotBeModifiedAfter(firstWriteTime);
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
                    .PublishProject(runtime: TestFixture.CurrentRid, 
                                    outputDirectory: GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
