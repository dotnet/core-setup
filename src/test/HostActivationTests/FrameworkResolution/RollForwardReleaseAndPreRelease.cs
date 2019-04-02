// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardReleaseAndPreRelease :
        FrameworkResolutionBase,
        IClassFixture<RollForwardReleaseAndPreRelease.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardReleaseAndPreRelease(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "4.1.2")]  // applyPatches is ignored
        [InlineData(Constants.RollForwardSetting.Major,       null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.0.1")]
        public void RollForwardOnPatch_FromReleaseIgnoresPreReleaseIfReleaseAvailable(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.Disable, false, "4.1.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable, true, "4.1.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, "4.1.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor, null, "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.Minor, false, "4.1.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major, null, "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.Major, false, "4.1.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        public void RollForwardOnPatch_FromExisting_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.1.1",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "4.1.2")]  // applyPatches is ignored
        [InlineData(Constants.RollForwardSetting.Major,       null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.0.1")]
        public void RollForwardOnMinor_FromReleaseIgnoresPreReleaseIfReleaseAvailable(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Major,       null,  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.0.1")]
        public void RollForwardOnMajor_FromReleaseIgnoresPreReleaseIfReleaseAvailable(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "3.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.5.2")]  // applyPatches is ignored
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.0.2-preview.1")] // Always considers all versions
        public void RollForwardOnPatch_FromPreReleaseToRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable, null, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.5.2")]  // applyPatches is ignored
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.0.2-preview.1")] // Always considers all versions
        public void RollForwardOnPatch_FromExisting_FromPreReleaseToRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.1",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Minor, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.5.2")]  // applyPatches is ignored
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.0.2-preview.1")]
        public void RollForwardOnMinor_FromPreReleaseToRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.0.0-preview.5",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Major, null, "5.1.2")]
        [InlineData(Constants.RollForwardSetting.Major, false, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null, "6.0.2-preview.1")]
        public void RollForwardOnMajor_FromPreReleaseToRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.9.0-preview.6",
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
                SharedState.DotNetWithNETCoreAppReleaseAndPreRelease,
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

            public DotNetCli DotNetWithNETCoreAppReleaseAndPreRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppReleaseAndPreRelease = DotNet("DotNetWithNETCoreAppReleaseAndPreRelease")

                    .AddMicrosoftNETCoreAppFramework("4.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.2")
                    .AddMicrosoftNETCoreAppFramework("4.1.3-preview.1")

                    .AddMicrosoftNETCoreAppFramework("4.5.1-preview.2")
                    .AddMicrosoftNETCoreAppFramework("4.5.2-preview.1")

                    .AddMicrosoftNETCoreAppFramework("5.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.2")

                    .AddMicrosoftNETCoreAppFramework("5.5.1-preview.2")
                    .AddMicrosoftNETCoreAppFramework("5.5.2")

                    .AddMicrosoftNETCoreAppFramework("6.0.1")
                    .AddMicrosoftNETCoreAppFramework("6.0.2-preview.1")

                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
