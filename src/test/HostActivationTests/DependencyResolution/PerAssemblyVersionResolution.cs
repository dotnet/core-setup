// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class PerAssemblyVersionResolution :
        ComponentDependencyResolutionBase,
        IClassFixture<PerAssemblyVersionResolution.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public PerAssemblyVersionResolution(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private const string TestDependencyResolverFx = "Test.DependencyResolver.Fx";
        private const string TestVersionsPackage = "Test.Versions.Package";

        // The test framework above has 4 assemblies in it each with different set of assembly and file versions.
        // The version values are always (if present)
        // - assembly version: 2.1.1.1
        // - file version:     3.2.2.2
        private const string TestAssemblyWithNoVersions = "Test.Assembly.NoVersions";
        private const string TestAssemblyWithAssemblyVersion = "Test.Assembly.AssemblyVersion";
        private const string TestAssemblyWithFileVersion = "Test.Assembly.FileVersion";
        private const string TestAssemblyWithBothVersions = "Test.Assembly.BothVersions";

        [Theory]
        [InlineData(TestAssemblyWithBothVersions, null, null, false)]
        [InlineData(TestAssemblyWithBothVersions, "1.0.0.0", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", "4.0.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.3.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.2.2.2", false)] // Lower level framework always wins on equality (this is intentional)
        [InlineData(TestAssemblyWithBothVersions, null, "4.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, null, "2.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", null, true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithNoVersions, null, null, false)] // No versions are treated as equal (so lower one wins)
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithNoVersions, null, "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "1.0.0.0", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "3.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, null, false)]
        [InlineData(TestAssemblyWithFileVersion, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithFileVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithFileVersion, null, "4.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, "3.2.2.2", false)]
        public void AppWithSameAssemblyAsFramework(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            var app = sharedTestState.CreateTestFrameworkReferenceApp(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));

            string expectedTestAssemblyPath =
                Path.Combine(appWins ? app.Location : sharedTestState.AppTestSharedFramework.Location, testAssemblyName + ".dll");

            sharedTestState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveResolvedAssembly(expectedTestAssemblyPath);
        }

        [Theory]
        [InlineData(TestAssemblyWithBothVersions, null, null, false)]
        [InlineData(TestAssemblyWithBothVersions, "1.0.0.0", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", "4.0.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.3.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.2.2.2", false)] // Lower level framework always wins on equality (this is intentional)
        [InlineData(TestAssemblyWithBothVersions, null, "4.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, null, "2.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", null, true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithNoVersions, null, null, false)] // No versions are treated as equal (so lower one wins)
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithNoVersions, null, "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "1.0.0.0", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "3.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, null, false)]
        [InlineData(TestAssemblyWithFileVersion, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithFileVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithFileVersion, null, "4.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, "3.2.2.2", false)]
        public void ComponentWithSameAssemblyAsFramework(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            var component = sharedTestState.CreateComponentWithNoDependencies(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));

            string expectedTestAssemblyPath =
                Path.Combine(appWins ? component.Location : sharedTestState.ComponentTestSharedFramework.Location, testAssemblyName + ".dll");

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[" +
                                          $"{component.AppDll}{Path.PathSeparator}" +
                                          $"{expectedTestAssemblyPath}{Path.PathSeparator}]");
        }

        public class SharedTestState : ComponentSharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp { get; }

            public TestApp AppTestSharedFramework { get; }
            public TestApp ComponentTestSharedFramework { get; }

            public SharedTestState()
            {
                // The simplest way to setup an assembly in framework we have full control over is to create a custom shared framework
                // We can't really mock Microsoft.NETCore.App since we need it to run the HostApiInvoker on.
                string sharedFrameworkPath = Path.Combine(
                    HostApiInvokerAppFixture.BuiltDotnet.BinPath,
                    "shared",
                    TestDependencyResolverFx,
                    "1.0.0");
                FileUtils.EnsureDirectoryExists(sharedFrameworkPath);

                ComponentTestSharedFramework = new TestApp(sharedFrameworkPath, TestDependencyResolverFx);
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(ComponentTestSharedFramework);
                PrepareTestFramework(builder);
                builder.Build(ComponentTestSharedFramework);

                // Modify the host API app to reference this test framework - since the component resolution is invoked by this app
                // Note: no need for backup, the shared test state instance creates a new instance of the app every time.
                RuntimeConfig.FromFile(HostApiInvokerAppFixture.TestProject.BuiltApp.RuntimeConfigJson)
                    .WithFramework(TestDependencyResolverFx, "1.0.0")
                    .Save();

                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(RepoDirectories.MicrosoftNETCoreAppVersion)
                    .AddFramework(TestDependencyResolverFx, "1.0.0", PrepareTestFramework)
                    .Build();
                AppTestSharedFramework = new TestApp(Path.Combine(DotNetWithNetCoreApp.BinPath, "shared", TestDependencyResolverFx, "1.0.0"));
            }

            private void PrepareTestFramework(NetCoreAppBuilder builder)
            {
                builder
                    .WithRuntimeConfig(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, RepoDirectories.MicrosoftNETCoreAppVersion))
                    .WithPackage(TestVersionsPackage, "1.0.0", b => b
                        .WithAssemblyGroup(null, g => g
                            .WithAsset(TestAssemblyWithNoVersions + ".dll")
                            .WithAsset(TestAssemblyWithAssemblyVersion + ".dll", rf => rf.WithVersion("2.1.1.1", null))
                            .WithAsset(TestAssemblyWithFileVersion + ".dll", rf => rf.WithVersion(null, "3.2.2.2"))
                            .WithAsset(TestAssemblyWithBothVersions + ".dll", rf => rf.WithVersion("2.1.1.1", "3.2.2.2"))));
            }

            public TestApp CreateTestFrameworkReferenceApp(Action<NetCoreAppBuilder> customizer)
            {
                TestApp testApp = CreateFrameworkReferenceApp(TestDependencyResolverFx, "1.0.0");
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(testApp);
                builder.WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly()));
                customizer(builder);
                return builder.Build(testApp);
            }

            public override void Dispose()
            {
                base.Dispose();

                ComponentTestSharedFramework.Dispose();
            }
        }
    }
}
