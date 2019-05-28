// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class ComponentActivation : IClassFixture<ComponentActivation.SharedTestState>
    {
        private const string ComponentActivationArg = "load_assembly_and_get_function_pointer";

        private readonly SharedTestState sharedState;

        public ComponentActivation(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        //[Theory]
        //[InlineData(true,  true,  true)]
        //[InlineData(false, true,  true)]
        //[InlineData(true,  false, true)]
        //[InlineData(true,  true,  false)]
        public void CallDelegate(bool validPath, bool validType, bool validMethod)
        {
            var componentProject = sharedState.ComponentWithNoDependenciesFixture.TestProject;
            string[] args =
            {
                ComponentActivationArg,
                sharedState.HostFxrPath,
                sharedState.RuntimeConfigPath,
                validPath ? componentProject.AppDll : "BadPath...",
                $"{(validType ? "Component.Component" : "Component.BadType")}, {componentProject.AssemblyName}",
                validMethod ? "ComponentEntryPoint" : "BadMethod",
            };
            CommandResult result = Command.Create(sharedState.NativeHostPath, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", sharedState.DotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", sharedState.DotNetRoot)
                .Execute();

            result.Should()
                .InitializeContextForConfig(sharedState.RuntimeConfigPath);

            if (validPath && validType && validMethod)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining("Called ComponentEntryPoint(0xdeadbeef, 42)");
            }
            else
            {
                result.Should().Fail();
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string DotNetRoot { get; }

            public string RuntimeConfigPath { get; }

            public TestProjectFixture ComponentWithNoDependenciesFixture { get; }

            public SharedTestState()
            {
                var dotNet = new Microsoft.DotNet.Cli.Build.DotNetCli(Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"));
                DotNetRoot = dotNet.BinPath;
                HostFxrPath = dotNet.GreatestVersionHostFxrFilePath;

                ComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                string configDir = Path.Combine(BaseDirectory, "config");
                Directory.CreateDirectory(configDir);
                RuntimeConfigPath = Path.Combine(configDir, "Component.runtimeconfig.json");
                RuntimeConfig.FromFile(RuntimeConfigPath)
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, RepoDirectories.MicrosoftNETCoreAppVersion))
                    .Save();
            }

            protected override void Dispose(bool disposing)
            {
                if (ComponentWithNoDependenciesFixture != null)
                    ComponentWithNoDependenciesFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
