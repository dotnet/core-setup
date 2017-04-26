// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.PortableApp
{
    public class GivenThatICareAboutDependencyModel
    {
        private static TestProjectFixture PreviouslyPublishedAndRestoredPortableTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutDependencyModel()
        {
            RepoDirectories = new RepoDirectoriesProvider();
            
            PreviouslyPublishedAndRestoredPortableTestProjectFixture = new TestProjectFixture("PackageCompilationAssemblyResolverTestApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();
        }

        

        [Fact]
        public void DependecyModel_gets_probingpaths_givento_Muxer()
        {
            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture
                .Copy();


            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var runtimeConfig = fixture.TestProject.RuntimeConfigJson;
            var additionalProbingPath = RepoDirectories.NugetPackages;

            dotnet.Exec(
                    "exec",
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .EnvironmentVariable("NUGET_PACKAGES","")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Succesfully resolved the assembly");
        }

    }
}
