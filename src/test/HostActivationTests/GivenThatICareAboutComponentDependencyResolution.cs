﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutComponentDependencyResolution : IClassFixture<GivenThatICareAboutComponentDependencyResolution.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private readonly ITestOutputHelper output;

        public GivenThatICareAboutComponentDependencyResolution(SharedTestState fixture, ITestOutputHelper output)
        {
            sharedTestState = fixture;
            this.output = output;
        }

        private const string corehost_resolve_component_dependencies = "corehost_resolve_component_dependencies";

        [Fact]
        public void InvalidMainComponentAssemblyPathFails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                fixture.TestProject.AppDll + "_invalid"
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x80008092]")
                .And.HaveStdErrContaining("Failed to locate managed application");
        }

        [Fact]
        public void ComponentWithNoDependenciesAndNoDeps()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{componentFixture.TestProject.AppDll}{Path.PathSeparator}]")
                .And.HaveStdErrContaining($"app_root='{componentFixture.TestProject.OutputDirectory}{Path.DirectorySeparatorChar}'")
                .And.HaveStdErrContaining($"deps='{componentFixture.TestProject.DepsJson}'")
                .And.HaveStdErrContaining($"mgd_app='{componentFixture.TestProject.AppDll}'")
                .And.HaveStdErrContaining($"-- arguments_t: dotnet shared store: '{Path.Combine(fixture.BuiltDotnet.BinPath, "store", sharedTestState.RepoDirectories.BuildArchitecture, fixture.Framework)}'");
        }

        [Fact]
        public void ComponentWithNoDependencies()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{componentFixture.TestProject.AppDll}{Path.PathSeparator}]");
        }

        private static readonly string[] SupportedOsList = new string[]
        {
            "ubuntu",
            "debian",
            "fedora",
            "opensuse",
            "osx",
            "rhel",
            "win"
        };

        private string GetExpectedLibuvRid(TestProjectFixture fixture)
        {
            // Simplified version of the RID fallback for libuv
            string currentRid = fixture.CurrentRid;
            string[] parts = currentRid.Split('-');
            string osName = parts[0];
            string architecture = parts[1];

            string supportedOsName = SupportedOsList.FirstOrDefault(a => osName.StartsWith(a));
            if (supportedOsName == null)
            {
                return null;
            }

            osName = supportedOsName;
            if (osName == "ubuntu") { osName = "debian"; }
            if (osName == "win") { osName = "win7"; }

            return osName + "-" + architecture;
        }

        [Fact]
        public void ComponentWithDependencies()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            string libuvRid = GetExpectedLibuvRid(componentFixture);
            if (libuvRid == null)
            {
                output.WriteLine($"RID {componentFixture.CurrentRid} is not supported by libuv and thus we can't run this test on it.");
                return;
            }

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies native_search_paths:[" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "runtimes", libuvRid, "native")}{Path.DirectorySeparatorChar}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndDependencyRemoved()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove a dependency
            // This will cause the resolution to fail
            File.Delete(Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808C]")
                .And.HaveStdErrContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) was not found:");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDeps()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDepsAndDependencyRemoved()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            // Remove a dependency
            // Since there's no .deps.json - there's no way for the system to know about this dependency and thus should not be reported.
            File.Delete(Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithSameDependencyWithDifferentExtensionShouldFail()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Add a reference to another package which has asset with the same name as the existing ComponentDependency
            // but with a different extension. This causes a failure.
            SharedFramework.AddReferenceToDepsJson(
                componentFixture.TestProject.DepsJson,
                "ComponentWithDependencies/1.0.0",
                "ComponentDependency_Dupe",
                "1.0.0",
                testAssembly: "ComponentDependency.notdll");

            // Make sure the file exists so that we avoid failing due to missing file.
            File.Copy(
                Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"),
                Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.notdll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808C]")
                .And.HaveStdErrContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) has already been found but with a different file extension")
                .And.HaveStdErrContaining("package: 'ComponentDependency_Dupe', version: '1.0.0'")
                .And.HaveStdErrContaining("path: 'ComponentDependency.notdll'")
                .And.HaveStdErrContaining($"previously found assembly: '{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}'");
        }

        [Fact]
        public void ComponentWithCorruptedDepsJsonShouldFail()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                componentFixture.TestProject.DepsJson,
                File.ReadAllLines(componentFixture.TestProject.DepsJson) + "}");

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808B]")
                .And.HaveStdErrContaining($"A JSON parsing exception occurred in [{componentFixture.TestProject.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdErrContaining($"Error initializing the dependency resolver: An error occurred while parsing: {componentFixture.TestProject.DepsJson}");
        }

        [Fact]
        public void ComponentWithResourcesShouldReportResourceSearchPaths()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{componentFixture.TestProject.OutputDirectory}{Path.DirectorySeparatorChar}{Path.PathSeparator}]");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableApiTestProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithDependenciesFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithResourcesFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public string BreadcrumbLocation { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredComponentWithDependenciesFixture = new TestProjectFixture("ComponentWithDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredComponentWithResourcesFixture = new TestProjectFixture("ComponentWithResources", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostpolicy, so copy it next to the app dll.
                    var fixture = PreviouslyPublishedAndRestoredPortableApiTestProjectFixture;
                    var hostpolicy = Path.Combine(
                        fixture.BuiltDotnet.GreatestVersionSharedFxPath,
                        $"{fixture.SharedLibraryPrefix}hostpolicy{fixture.SharedLibraryExtension}");

                    File.Copy(
                        hostpolicy,
                        Path.GetDirectoryName(fixture.TestProject.AppDll));
                }
            }

            public void Dispose()
            {
                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Dispose();
            }
        }
    }
}