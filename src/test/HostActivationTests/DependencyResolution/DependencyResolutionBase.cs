// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class DependencyResolutionBase
    {
        protected const string MicrosoftNETCoreApp = "Microsoft.NETCore.App";

        public abstract class SharedTestStateBase : TestArtifact
        {
            protected string BuiltDotnetPath { get; }

            private static string GetBaseDir(string name)
            {
                string baseDir = Path.Combine(TestArtifactsPath, name);
                return SharedFramework.CalculateUniqueTestDirectory(baseDir);
            }

            public SharedTestStateBase()
                : base(GetBaseDir("dependencyResolution"), "dependencyResolution")
            {
                BuiltDotnetPath = Path.Combine(TestArtifactsPath, "sharedFrameworkPublish");
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(Location, BuiltDotnetPath, name);
            }

            public TestApp CreateFrameworkReferenceApp(string fxName, string fxVersion)
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                string testAppDir = Path.Combine(Location, "FrameworkReferenceApp");
                Directory.CreateDirectory(testAppDir);

                TestApp testApp = new TestApp(testAppDir);
                RuntimeConfig.Path(testApp.RuntimeConfigJson)
                    .WithFramework(fxName, fxVersion)
                    .Save();

                return testApp;
            }
        }
    }
}
