// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardPreReleaseOnly :
        FrameworkResolutionBase,
        IClassFixture<RollForwardPreReleaseOnly.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardPreReleaseOnly(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnPatch_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnMinor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, null)]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false, "6.1.0-preview.2")] // applyPatches is ignored for new rollForward settings
        public void RollForwardOnMajor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.0.0",
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
        public void NeverRollBackOnPreRelease_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.2-preview.3",
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
        public void NeverRollBackOnPatch_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.3-preview.1",
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
        public void NeverRollBackOnMinor_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.3.0-preview.1",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnPreRelease_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.2-preview.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnPatch_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.1",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnPatch_FromExisting_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.1-preview.1",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        public void RollForwardOnMinor_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.0.0-preview.5",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, null)]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.1.0-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false, "6.1.0-preview.2")] // applyPatches is ignored for new rollForward settings
        public void RollForwardOnMajor_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.1.0-preview.6",
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
                SharedState.DotNetWithNETCoreAppPreRelease,
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

            public DotNetCli DotNetWithNETCoreAppPreRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppPreRelease = DotNet("DotNetWithNETCoreAppPreRelease")
                    .AddMicrosoftNETCoreAppFramework("5.1.1-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.2-preview.2")
                    .AddMicrosoftNETCoreAppFramework("5.2.0-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.2.1-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.2.1-preview.2")
                    .AddMicrosoftNETCoreAppFramework("6.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFramework("6.1.0-preview.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}