// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardMultipleFrameworks :
        FrameworkResolutionBase,
        IClassFixture<RollForwardMultipleFrameworks.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";
        private const string AnotherMiddleWare = "AnotherMiddleWare";
        private const string HighWare = "HighWare";

        private SharedTestState SharedState { get; }

        public RollForwardMultipleFrameworks(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithMultipleFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithMultipleFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFramework("5.1.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddMicrosoftNETCoreAppFramework("5.4.1")
                    .AddMicrosoftNETCoreAppFramework("5.6.0")
                    .AddMicrosoftNETCoreAppFramework("6.0.0")
                    .AddMicrosoftNETCoreAppFramework("6.1.0")
                    .AddMicrosoftNETCoreAppFramework("6.1.1-preview.2")
                    .AddMicrosoftNETCoreAppFramework("6.2.1")
                    .AddFramework(MiddleWare, "2.1.2", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .AddFramework(AnotherMiddleWare, "3.0.0", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .AddFramework(HighWare, "7.3.1", runtimeConfig =>
                        runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.3")
                            .WithFramework(MiddleWare, "2.1.2"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is higher.
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.0.0",      Constants.RollForwardSetting.Disable,     null)]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Disable,     "5.1.1")]
        [InlineData("5.1.3",      Constants.RollForwardSetting.Disable,     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData("5.1.0",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.0.0",      null,                                     "5.1.3")]
        [InlineData("5.1.1",      null,                                     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestMinor, "5.1.3")] // The app reference which is Minor wins
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMinor, "5.1.3")] // The app reference which is Minor wins
        [InlineData("1.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Major,       "5.1.3")] // The app reference which is Minor wins
        [InlineData("1.0.0",      Constants.RollForwardSetting.LatestMajor, "5.1.3")] // The app reference which is Minor wins
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToHigher(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is higher.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.0.0",      Constants.RollForwardSetting.Disable,     null)]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Disable,     "5.1.1")]
        [InlineData("5.1.3",      Constants.RollForwardSetting.Disable,     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData("5.1.0",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.0.0",      null,                                     "5.1.3")]
        [InlineData("5.1.1",      null,                                     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestMinor, "5.1.3")] // The app reference which is Minor wins
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMinor, "5.1.3")] // The app reference which is Minor wins
        [InlineData("1.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Major,       "5.1.3")] // The app reference which is Minor wins
        [InlineData("1.0.0",      Constants.RollForwardSetting.LatestMajor, "5.1.3")] // The app reference which is Minor wins
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is lower.
        // Also validates that since all relevant available versions are release, 
        // the DOTNET_ROLL_FORWARD_TO_PRERELEASE has no effect on the result.
        [Theory] // fxRefVersion  rollForward                               rollForwadToPreRelease resolvedFramework
        [InlineData("5.4.0",      null,                                     false,                 "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       false,                 "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       true,                  "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, false,                 "5.4.1")] // The app's settings (Minor) wins, so effective reference is "5.4.0 Minor"
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, true,                  "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       false,                 "5.4.1")] // The app's settings (Minor) wins, so effective reference is "5.4.0 Minor"
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       true,                  "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, false,                 "5.4.1")] // The app's settings (Minor) wins, so effective reference is "5.4.0 Minor"
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, true,                  "5.4.1")]
        [InlineData("5.4.1",      Constants.RollForwardSetting.Disable,     false,                 "5.4.1")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.Minor,       false,                 "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.Minor,       true,                  "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.LatestMinor, false,                 "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.Major,       false,                 "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.LatestMajor, false,                 "[not found]")]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       false,                 null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       true,                  null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Major,       false,                 null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.LatestMajor, false,                 null)]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToLower(
            string versionReference,
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            CommandResult result = RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference),
                rollForwardToPreRelease);

            if (resolvedFramework == "[not found]")
            {
                result.ShouldFailToFindCompatibleFrameworkVersion();
            }
            else
            {
                result.ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
            }
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is lower.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.4.0",      null,                                     "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, "5.4.1")]
        [InlineData("5.4.1",      Constants.RollForwardSetting.Disable,     "5.4.1")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.Minor,       "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.LatestMinor, "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.Major,       "[not found]")]
        [InlineData("5.7.0",      Constants.RollForwardSetting.LatestMajor, "[not found]")]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Major,       null)]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToLower_HardResolve(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            CommandResult result = RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference));

            if (resolvedFramework == "[not found]")
            {
                result.ShouldFailToFindCompatibleFrameworkVersion();
            }
            else
            {
                result.ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
            }
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 6.1.1-preview.0 (defaults = RollForward:Minor).
        // Also validates the effect of DOTNET_ROLL_FORWARD_TO_PRERELEASE on the result.
        [Theory] // fxRefVersion       rollForward                               rollForwadToPreRelease resolvedFramework
        [InlineData("6.0.0-preview.1", null,                                     false,                 "6.1.1-preview.2")]
        [InlineData("6.0.0",           null,                                     false,                 "6.2.1")]
        [InlineData("6.0.0",           Constants.RollForwardSetting.LatestPatch, false,                 null)]
        [InlineData("6.0.0-preview.1", Constants.RollForwardSetting.LatestPatch, false,                 null)]
        [InlineData("6.0.0-preview.1", Constants.RollForwardSetting.Minor,       false,                 "6.1.1-preview.2")]
        [InlineData("6.0.0",           Constants.RollForwardSetting.Minor,       false,                 "6.2.1")]
        [InlineData("6.0.1-preview.0", Constants.RollForwardSetting.LatestPatch, false,                 null)]
        [InlineData("6.1.0-preview.0", null,                                     false,                 "6.1.1-preview.2")]
        [InlineData("6.1.0-preview.0", null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.0",           null,                                     false,                 "6.2.1")]
        [InlineData("6.1.0",           null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", null,                                     false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", Constants.RollForwardSetting.LatestPatch, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", Constants.RollForwardSetting.Disable,     false,                 "[not found]")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Disable,     false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Disable,     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestPatch, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestPatch, true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", null,                                     false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Minor,       false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Minor,       true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMinor, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMinor, true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Major,       false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Major,       true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMajor, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMajor, true,                  "6.1.1-preview.2")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Disable,     false,                 "[not found]")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestPatch, false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", null,                                     false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Minor,       false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestMinor, false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Major,       false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestMajor, false,                 "6.2.1")]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_PreRelease(
            string versionReference,
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            CommandResult result = RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.1.1-preview.0")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference),
                rollForwardToPreRelease);

            if (resolvedFramework == "[not found]")
            {
                result.ShouldFailToFindCompatibleFrameworkVersion();
            }
            else
            {
                result.ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "6.1.1-preview.0");
            }
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForward>)
        // is correctly reconciled with app's framework reference 6.1.0 (defaults = RollForward:Minor).
        // Also validates the effect of DOTNET_ROLL_FORWARD_TO_PRERELEASE on the result.
        [Theory] // fxRefVersion       rollForward                               rollForwadToPreRelease resolvedFramework
        [InlineData("6.0.0",           null,                                     false,                 "6.1.0")]
        [InlineData("6.0.0",           null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.0.0",           Constants.RollForwardSetting.LatestPatch, false,                 null)]
        [InlineData("6.0.0",           Constants.RollForwardSetting.Minor,       false,                 "6.1.0")]
        [InlineData("6.0.0",           Constants.RollForwardSetting.Minor,       true,                  "6.1.1-preview.2")]
        [InlineData("6.0.1-preview.0", Constants.RollForwardSetting.LatestPatch, false,                 null)]
        [InlineData("6.1.0",           null,                                     false,                 "6.1.0")]
        [InlineData("6.1.0",           null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", null,                                     false,                 "6.2.1")]
        [InlineData("6.1.1-preview.0", null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", Constants.RollForwardSetting.Disable,     false,                 "[not found]")]
        [InlineData("6.1.1-preview.0", Constants.RollForwardSetting.LatestPatch, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Disable,     false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Disable,     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestPatch, false,                 "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestPatch, true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", null,                                     false,                 "6.2.1")]
        [InlineData("6.1.1-preview.2", null,                                     true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Minor,       false,                 "6.2.1")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Minor,       true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMinor, false,                 "6.2.1")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMinor, true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Major,       false,                 "6.2.1")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.Major,       true,                  "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMajor, false,                 "6.2.1")]
        [InlineData("6.1.1-preview.2", Constants.RollForwardSetting.LatestMajor, true,                  "6.1.1-preview.2")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Disable,     false,                 "[not found]")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestPatch, false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", null,                                     false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Minor,       false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestMinor, false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.Major,       false,                 "6.2.1")]
        [InlineData("6.2.1-preview.1", Constants.RollForwardSetting.LatestMajor, false,                 "6.2.1")]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_Release(
            string versionReference,
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            CommandResult result = RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.1.0")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForward(rollForward)
                        .Version = versionReference),
                rollForwardToPreRelease);

            if (resolvedFramework == "[not found]")
            {
                result.ShouldFailToFindCompatibleFrameworkVersion();
            }
            else
            {
                result.ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "6.1.0");
            }
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForward>).
        // App fx reference is lower.
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.0.0",      Constants.RollForwardSetting.Disable,     null)]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Disable,     "5.1.1")]
        [InlineData("5.1.3",      Constants.RollForwardSetting.Disable,     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData("5.1.0",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.0.0",      null,                                     "5.1.3")]
        [InlineData("5.1.1",      null,                                     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Major,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Major,       "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.LatestMajor, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMajor, "5.1.3")]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToLower(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForward(rollForward)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForward>).
        // App fx reference is lower.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.0.0",      Constants.RollForwardSetting.Disable,     null)]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Disable,     "5.1.1")]
        [InlineData("5.1.3",      Constants.RollForwardSetting.Disable,     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData("5.1.0",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.0.0",      null,                                     "5.1.3")]
        [InlineData("5.1.1",      null,                                     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Major,       "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.LatestMajor, "5.1.3")]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToLower_HardResolve(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForward(rollForward))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                // Note that in this case (since the app reference is first) if the app's framework reference
                // can't be resolved against the available frameworks, the error is actually a regular
                // "can't find framework" and not a framework reconcile event.
                .ShouldHaveResolvedFrameworkOrFail(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForward>).
        // App fx reference is higher.
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.4.0",      null,                                     "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, "5.4.1")]
        [InlineData("5.4.1",      Constants.RollForwardSetting.Disable,     "5.4.1")]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Major,       null)]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToHigher(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForward(rollForward)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForward>).
        // App fx reference is higher.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.4.0",      null,                                     "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, "5.4.1")]
        [InlineData("5.4.1",      Constants.RollForwardSetting.Disable,     "5.4.1")]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Major,       null)]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForward(rollForward))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with another framwork's framework reference (<fxRefVersion>, <rollForward>).
        // The higher framework has fx reference with higher version.
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.0.0",      Constants.RollForwardSetting.Disable,     null)]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Disable,     "5.1.1")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData("5.1.0",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestPatch, "5.1.3")]
        [InlineData("5.0.0",      null,                                     "5.1.3")]
        [InlineData("5.1.1",      null,                                     "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.Minor,       "5.1.3")]
        [InlineData("5.0.0",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("5.1.1",      Constants.RollForwardSetting.LatestMinor, "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("1.0.0",      Constants.RollForwardSetting.Major,       "5.1.3")]
        [InlineData("1.0.0",      Constants.RollForwardSetting.LatestMajor, "5.1.3")]
        public void ReconcileFrameworkReferences_InnerToInnerFrameworkReference_ToLower(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(HighWare, "7.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.1.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .WithRollForward(rollForward)
                            .Version = versionReference);
                })
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with another framwork's framework reference (<fxRefVersion>, <rollForward>).
        // The higher framework has fx reference with lower version.
        [Theory] // fxRefVersion  rollForward                               resolvedFramework
        [InlineData("5.1.3",      Constants.RollForwardSetting.Disable,     "5.1.3")]
        [InlineData("5.4.0",      null,                                     "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Minor,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMinor, "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.Major,       "5.4.1")]
        [InlineData("5.4.0",      Constants.RollForwardSetting.LatestMajor, "5.4.1")]
        [InlineData("5.4.1",      Constants.RollForwardSetting.Disable,     "5.4.1")]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Minor,       null)]
        [InlineData("6.0.0",      Constants.RollForwardSetting.Major,       null)]
        public void ReconcileFrameworkReferences_InnerToInnerFrameworkReference_ToHigher(
            string versionReference,
            string rollForward,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(HighWare, "7.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.1.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .WithRollForward(rollForward)
                            .Version = versionReference);
                })
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // This test:
        //  - Forces hard resolve of 5.1.1 -> 5.1.3 (direct reference from app)
        //  - Loads HighWare which has 5.4.1 
        //    - This forces a retry since 5.1.3 was hard resolved, so we have reload with 5.4.1 instead
        //  - Loads MiddleWare which has 5.6.0
        //    - This forces a retry since by this time 5.4.1 was hard resolved, so we have to reload with 5.6.0 instead
        [Fact]
        public void FrameworkResolutionRetry_FrameworkChain()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForward(Constants.RollForwardSetting.Major)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(HighWare, "7.3.1"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.4.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.6.0");
                })
                .Should().Pass()
                .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0");
        }

        // This test:
        //  - Forces hard resolve of 5.1.1 -> 5.1.3 (direct reference from app)
        //  - Loads MiddleWare which has 5.4.1 
        //    - This forces a retry since 5.1.3 was hard resolved, so we have reload with 5.4.1 instead
        //  - Loads AnotherMiddleWare which has 5.6.0
        //    - This forces a retry since by this time 5.4.1 was hard resolved, so we have to reload with 5.6.0 instead
        [Fact]
        public void FrameworkResolutionRetry_FrameworkTree()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForward(Constants.RollForwardSetting.Major)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.2")
                    .WithFramework(AnotherMiddleWare, "3.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.4.1");
                    dotnetCustomizer.Framework(AnotherMiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.6.0");
                })
                .Should().Pass()
                .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0");
        }

        // Verifies that reconciling framework references correctly remembers whether it should prefer release versions or not.
        [Theory]
        [InlineData("6.0.0",           "6.1.1-preview.0", "6.2.1")]           // Release should prefer release even if there's a pre-release in the middle
        [InlineData("6.1.0",           "6.1.1-preview.0", "6.2.1")]           // Release should prefer release even if there's a pre-release in the middle
        [InlineData("6.1.1",           "6.1.1-preview.0", "6.2.1")]           // Release should prefer release even if there's a pre-release in the middle
        [InlineData("6.0.0-preview.1", "6.1.1-preview.0", "6.1.1-preview.2")] // Both pre-relelase, take the closest even if it's pre-release
        [InlineData("6.1.0-preview.0", "6.1.1",           "6.2.1")]           // Release should prefer release
        [InlineData("6.1.1-preview.0", "6.1.0",           "6.2.1")]           // Release should prefer release
        [InlineData("6.1.1-preview.0", "6.1.1",           "6.2.1")]           // Release should prefer release
        public void PreferReleaseToRelease(string appVersionReference, string frameworkVersionReference, string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.2")
                    .WithFramework(MicrosoftNETCoreApp, appVersionReference),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = frameworkVersionReference);
                })
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        private CommandResult RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            bool rollForwardToPreRelease = false)
        {
            return RunTest(
                SharedState.DotNetWithMultipleFrameworks,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig)
                    .WithDotnetCustomizer(customizeDotNet)
                    .WithEnvironment(Constants.RollForwardToPreRelease.EnvironmentVariable, rollForwardToPreRelease ? "1" : "0"));
        }
    }
}
