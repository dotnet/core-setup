// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardReleaseOnly :
        FrameworkResolutionBase,
        IClassFixture<RollForwardReleaseOnly.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardReleaseOnly(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Fact]
        public void ExactMatchOnRelease()
        {
            RunTest(
                "2.1.3",
                null,
                null,
                "2.1.3");
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.Disable, false, null)]    // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable, true, null)]    // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]    // Backward compat, equivalient to rollForwardOnNoCadidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "2.4.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "2.4.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "3.1.2")]
        public void RollForwardOnPatch_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, "2.1.2")]
        [InlineData(Constants.RollForwardSetting.Disable, false, "2.1.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable, true, "2.1.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        public void RollForwardOnPatch_FromExisting_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.1.2",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "2.4.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "2.4.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "3.1.2")]
        public void RollForwardOnMinor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, null)]
        [InlineData(Constants.RollForwardSetting.Major, null, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major, false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "3.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false, "3.1.2")] // applyPatches is ignored for new rollForward settings
        public void RollForwardOnMajor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "1.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.Disable, false, null)]
        [InlineData(Constants.RollForwardSetting.Disable, true, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]
        public void NeverRollBackOnPatch_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.1.4",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, false, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, null)]
        public void NeverRollBackOnMinor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.4.2",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        private void RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            bool? applyPatches,
            string resolvedFrameworkVersion)
        {
            RunTest(
                SharedState.DotNetWithNETCoreAppRelease,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithApplyPatches(applyPatches)
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)),
                MicrosoftNETCoreApp,
                resolvedFrameworkVersion);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNETCoreAppRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppRelease = DotNet("DotNetWithNETCoreAppRelease")
                    .AddMicrosoftNETCoreAppFramework("2.1.2")
                    .AddMicrosoftNETCoreAppFramework("2.1.3")
                    .AddMicrosoftNETCoreAppFramework("2.4.0")
                    .AddMicrosoftNETCoreAppFramework("2.4.1")
                    .AddMicrosoftNETCoreAppFramework("3.1.1")
                    .AddMicrosoftNETCoreAppFramework("3.1.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}