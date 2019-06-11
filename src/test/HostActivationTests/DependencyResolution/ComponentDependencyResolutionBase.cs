// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class ComponentDependencyResolutionBase : DependencyResolutionBase
    {
        public abstract class ComponentSharedTestStateBase : SharedTestStateBase
        {
            private const string corehost_resolve_component_dependencies = "corehost_resolve_component_dependencies";
            private const string corehost_resolve_component_dependencies_multithreaded = "corehost_resolve_component_dependencies_multithreaded";

            public TestProjectFixture HostApiInvokerAppFixture { get; }

            public ComponentSharedTestStateBase()
            {
                HostApiInvokerAppFixture = CreateHostApiInvokerApp();
            }

            private TestProjectFixture CreateHostApiInvokerApp()
            {
                TestProjectFixture hostApiInvokerAppFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostpolicy, so copy it next to the app dll.
                    var hostpolicy = Path.Combine(
                        hostApiInvokerAppFixture.BuiltDotnet.GreatestVersionSharedFxPath,
                        RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));

                    File.Copy(
                        hostpolicy,
                        Path.GetDirectoryName(hostApiInvokerAppFixture.TestProject.AppDll));
                }

                return hostApiInvokerAppFixture;
            }

            public CommandResult RunComponentResolutionTest(TestApp component, Action<Command> commandCustomizer = null)
            {
                return RunComponentResolutionTest(component.AppDll, commandCustomizer);
            }

            public CommandResult RunComponentResolutionTest(string componentPath, Action<Command> commandCustomizer = null)
            {
                string[] args =
                {
                corehost_resolve_component_dependencies,
                componentPath
            };

                Command command = HostApiInvokerAppFixture.BuiltDotnet.Exec(HostApiInvokerAppFixture.TestProject.AppDll, args)
                    .EnableTracingAndCaptureOutputs();
                commandCustomizer?.Invoke(command);

                return command.Execute()
                    .StdErrAfter("corehost_resolve_component_dependencies = {");
            }

            public CommandResult RunComponentResolutionMultiThreadedTest(TestApp componentOne, TestApp componentTwo)
            {
                return RunComponentResolutionMultiThreadedTest(componentOne.AppDll, componentTwo.AppDll);
            }

            public CommandResult RunComponentResolutionMultiThreadedTest(string componentOnePath, string componentTwoPath)
            {
                string[] args =
                {
                corehost_resolve_component_dependencies_multithreaded,
                componentOnePath,
                componentTwoPath
            };
                return HostApiInvokerAppFixture.BuiltDotnet.Exec(HostApiInvokerAppFixture.TestProject.AppDll, args)
                    .EnableTracingAndCaptureOutputs()
                    .Execute();
            }

            public override void Dispose()
            {
                base.Dispose();

                HostApiInvokerAppFixture.Dispose();
            }
        }
    }
}
