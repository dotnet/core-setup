// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using BundleTests.Helpers;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundleSignTests : IClassFixture<BundleSignTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleSignTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void MockSign(string fileName)
        {
            using (var file = File.OpenWrite(fileName))
            {
                file.Position = file.Length;
                var blob = Encoding.UTF8.GetBytes("Mock signature at the end of the bundle");
                file.Write(blob, 0, blob.Length);
            }
        }

        [Fact]
        public void RunBundleWithAdditionalContentAfterMetadata()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            var bundler = new Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(BundleHelper.GetPublishPath(fixture));

            MockSign(singleFile);

            Command.Create(singleFile)
                   .CaptureStdErr()
                   .CaptureStdOut()
                   .Execute()
                   .Should()
                   .Pass()
                   .And
                   .HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void ExtractBundleWithAdditionalContentAfterMetadata()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            var bundler = new Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(BundleHelper.GetPublishPath(fixture));

            MockSign(singleFile);

            Extractor extractor = new Extractor(singleFile, bundleDir.FullName);
            extractor.ExtractFiles();
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("PortableApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid,
                                    selfContained: "false",
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
