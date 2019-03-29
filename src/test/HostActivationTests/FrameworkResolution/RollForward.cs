// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
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

        [Fact]
        public void ExactMatchOnRelease()
        {
            RunTestWithNETCoreAppRelease(
                "2.1.3",
                null,
                null,
                "2.1.3");
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.Disable,     false, null)]    // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable,     true,  null)]    // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]    // Backward compat, equivalient to rollForwardOnNoCadidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "2.4.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "2.4.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major,       null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "3.1.2")]
        public void RollForwardOnPatch_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppRelease(
                "2.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "2.4.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "2.4.1")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major,       null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "3.1.2")]
        public void RollForwardOnMinor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppRelease(
                "2.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  null)]
        [InlineData(Constants.RollForwardSetting.Major,       null,  "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "2.1.2")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "3.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false, "3.1.2")] // applyPatches is ignored for new rollForward settings
        public void RollForwardOnMajor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppRelease(
                "1.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false, null)]              // Backward compat, equivalient to rollForwardOnNoCadidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "5.1.1-preview.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major,       null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "5.1.1-preview.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.1.0-preview.2")]
        public void RollForwardOnPatch_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppPreRelease(
                "5.1.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       false, "5.1.1-preview.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false, "5.2.1-preview.2")] // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Major,       null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "5.1.1-preview.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.1.0-preview.2")]
        public void RollForwardOnMinor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppPreRelease(
                "5.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable,     null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,  null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,  null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,  null)]
        [InlineData(Constants.RollForwardSetting.Major,       null,  "5.1.2-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       false, "5.1.1-preview.1")] // Backward compat, equivalient to rollForwardOnNoCadidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,  "6.1.0-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false, "6.1.0-preview.2")] // applyPatches is ignored for new rollForward settings
        public void RollForwardOnMajor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithNETCoreAppPreRelease(
                "4.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
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
            RunTestWithNETCoreAppReleaseAndPreRelease(
                "4.1.0",
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
            RunTestWithNETCoreAppReleaseAndPreRelease(
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
            RunTestWithNETCoreAppReleaseAndPreRelease(
                "3.0.0",
                rollForward,
                applyPatches,
                resolvedFramework);
        }

        private void RunTestWithNETCoreAppRelease(string frameworkReferenceVersion, string rollForward, bool? applyPatches, string resolvedFrameworkVersion)
            => RunTestWithNETCoreApp(SharedState.DotNetWithNETCoreAppRelease, frameworkReferenceVersion, rollForward, applyPatches, resolvedFrameworkVersion);

        private void RunTestWithNETCoreAppPreRelease(string frameworkReferenceVersion, string rollForward, bool? applyPatches, string resolvedFrameworkVersion)
            => RunTestWithNETCoreApp(SharedState.DotNetWithNETCoreAppPreRelease, frameworkReferenceVersion, rollForward, applyPatches, resolvedFrameworkVersion);

        private void RunTestWithNETCoreAppReleaseAndPreRelease(string frameworkReferenceVersion, string rollForward, bool? applyPatches, string resolvedFrameworkVersion)
            => RunTestWithNETCoreApp(SharedState.DotNetWithNETCoreAppReleaseAndPreRelease, frameworkReferenceVersion, rollForward, applyPatches, resolvedFrameworkVersion);

        private void RunTestWithNETCoreApp(
            DotNetCli dotNet,
            string frameworkReferenceVersion,
            string rollForward,
            bool? applyPatches,
            string resolvedFrameworkVersion)
        {
            RunTest(
                dotNet,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithApplyPatches(applyPatches)
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)),
                result =>
                {
                    if (resolvedFrameworkVersion == null)
                    {
                        result.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                    else
                    {
                        result.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFrameworkVersion);
                    }
                });
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNETCoreAppRelease { get; }

            public DotNetCli DotNetWithNETCoreAppPreRelease { get; }

            public DotNetCli DotNetWithNETCoreAppReleaseAndPreRelease { get; }

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

                DotNetWithNETCoreAppReleaseAndPreRelease = DotNet("DotNetWithNETCoreAppReleaseAndPreRelease")

                    .AddMicrosoftNETCoreAppFramework("4.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.2")
                    .AddMicrosoftNETCoreAppFramework("4.1.3-preview.1")

                    .AddMicrosoftNETCoreAppFramework("4.5.1-preview.2")
                    .AddMicrosoftNETCoreAppFramework("4.5.2-preview.1")

                    .AddMicrosoftNETCoreAppFramework("6.0.1")
                    .AddMicrosoftNETCoreAppFramework("6.0.2-preview.1")

                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
