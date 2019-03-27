// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForward :
        FrameworkResolutionBase,
        IClassFixture<RollForward.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForward(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor, "5.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.4.1")]
        [InlineData(Constants.RollForwardSetting.Major, "5.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.2")]
        public void ReleaseToRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithRollForward(rollForward)
                        .WithFramework(MicrosoftNETCoreApp, "5.1.2")),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework));
        }

        private void RunTest(
            TestSettings testSettings,
            Action<CommandResult> resultAction)
        {
            RunTest(
                SharedState.DotNetWithFrameworks,
                SharedState.FrameworkReferenceApp,
                testSettings,
                resultAction);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFramework("5.1.2")
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddMicrosoftNETCoreAppFramework("5.4.0")
                    .AddMicrosoftNETCoreAppFramework("5.4.1")
                    .AddMicrosoftNETCoreAppFramework("6.1.1")
                    .AddMicrosoftNETCoreAppFramework("6.1.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
