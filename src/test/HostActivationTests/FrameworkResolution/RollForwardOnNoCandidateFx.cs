// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardOnNoCandidateFx : 
        FrameworkResolutionBase,
        IClassFixture<RollForwardOnNoCandidateFx.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardOnNoCandidateFx(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        #region With one release framework
        // RunTestWithOneFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3

        [Fact]
        public void ExactMatchOnRelease_NoSettings()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(0, null)]
        [InlineData(1, null)]
        [InlineData(1, false)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2, null)]
        [InlineData(2, false)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardToLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, false, true)]
        [InlineData(0, null, false)]
        [InlineData(1, null, true)]
        [InlineData(1, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardOnMinor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(0, null, false)]
        [InlineData(1, null, false)]
        [InlineData(1, false, false)]
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardOnMajor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(0, false)]
        [InlineData(1, null)]
        [InlineData(1, false)]
        [InlineData(2, null)]
        [InlineData(2, false)]
        public void NeverRollBackOnRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.4"),
                commandResult => commandResult.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_FailsToRollPatches()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult => commandResult.Should().Fail()
                    .And.HaveStdErrContaining("Did not roll forward because apply_patches=0, roll_forward=1 chose [5.1.0]"));
        }

        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_MatchesExact()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void RollForwardOnMinorDisabledOnNoCandidateFx_FailsToRoll()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                // Will still attempt roll forward to latest patch
                commandResult => commandResult.Should().Fail()
                    .And.HaveStdErrContaining("Attempting FX roll forward")
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        // 3.0 change: In 2.* pre-release never rolled to release. In 3.* it will follow normal roll-forward rules.
        [Fact]
        public void PreReleaseReference_FailsToRollToRelease()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0-preview.1"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        private void RunTestWithOneFramework(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithOneFramework, runtimeConfig, resultAction);
        }
        #endregion

        #region With one pre-release framework
        // RunTestWithPreReleaseFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3-preview.2

        [Fact]
        public void ExactMatchOnPreRelease_NoSettings()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"));
        }

        // 3.0 change:
        // 2.* - Pre-Release only rolls on the exact same major.minor.patch (it only rolls over the pre-release portion of the version)
        // 3.* - Pre-Release follows normal roll-forward rules, including rolling over patches
        [Fact]
        public void RollForwardToPreRelease_FailsOnVersionMismatch()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2-preview.2"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"));
        }

        [Theory]
        [InlineData(null, null, "5.1.3-preview.2")]
        // 3.0 change:
        // 2.* - Pre-Release ignores roll forward on no candidate FX and apply patches settings
        // 3.* - Pre-Release follows normal roll-forward rules, including all the roll-forward settings
        //   with the exception of applyPatches=false for pre-release roll.
        [InlineData(0, false, "5.1.3-preview.2")]
        [InlineData(2, true, "5.1.3-preview.2")]
        public void RollForwardToPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedFramework)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.1"),
                commandResult =>
                {
                    if (resolvedFramework != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, true)]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0, null, true)]
        [InlineData(1, null, true)]
        [InlineData(1, false, true)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2, null, true)]
        [InlineData(2, false, true)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardToPreReleaseLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, false, true)]
        [InlineData(0, null, false)]
        [InlineData(1, null, true)]
        [InlineData(1, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardToPreReleaseOnMinor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(0, null, false)]
        [InlineData(1, null, false)]
        [InlineData(1, false, false)]
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        public void RollForwardToPreReleaseOnMajor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(0, false)]
        [InlineData(1, null)]
        [InlineData(1, false)]
        [InlineData(2, null)]
        [InlineData(2, false)]
        public void NeverRollBackOnPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.9"),
                commandResult => commandResult.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        private void RunTestWithPreReleaseFramework(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithPreReleaseFramework, runtimeConfig, resultAction);
        }
        #endregion

        #region With many versions
        // RunWithManyVersions has these frameworks
        //  - Microsoft.NETCore.App 2.3.1-preview.1
        //  - Microsoft.NETCore.App 2.3.2
        //  - Microsoft.NETCore.App 4.1.1
        //  - Microsoft.NETCore.App 4.1.2
        //  - Microsoft.NETCore.App 4.1.3-preview.1
        //  - Microsoft.NETCore.App 4.2.1
        //  - Microsoft.NETCore.App 4.5.1-preview.1
        //  - Microsoft.NETCore.App 4.5.2
        //  - Microsoft.NETCore.App 5.1.3-preview.1
        //  - Microsoft.NETCore.App 5.1.3-preview.2
        //  - Microsoft.NETCore.App 5.1.4-preview.1
        //  - Microsoft.NETCore.App 5.2.3-preview.1
        //  - Microsoft.NETCore.App 5.2.3-preview.2
        //  - Microsoft.NETCore.App 6.1.1
        //  - Microsoft.NETCore.App 6.1.2-preview.1
        //  - Microsoft.NETCore.App 7.1.1-preview.1
        //  - Microsoft.NETCore.App 7.1.2-preview.1

        [Theory]
        [InlineData(null, null, "4.1.2")]
        [InlineData(null, false, "4.1.1")]
        [InlineData(0, null, "4.1.2")]
        [InlineData(0, false, "4.1.1")]  // No roll forward
        [InlineData(1, null, "4.1.2")]
        [InlineData(1, false, "4.1.1")]  // Doesn't roll to latest patch
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]  // Doesn't roll to latest patch
        public void RollForwardToLatestPatch_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.1"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion));
        }

        [Theory]
        [InlineData(null, null, "4.1.2")]
        [InlineData(null, false, "4.1.1")]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, "4.1.2")]
        [InlineData(1, false, "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        public void RollForwardOnMinor_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "4.5.2")]
        [InlineData(null, false, "4.5.2")]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, "4.5.2")]
        [InlineData(1, false, "4.5.2")]
        [InlineData(2, null, "4.5.2")]
        [InlineData(2, false, "4.5.2")]
        public void RollForwardOnMinor_RollOverPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.4.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, false, null)]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, null)]
        [InlineData(1, false, null)]
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        public void RollForwardOnMajor_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "3.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.1")]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0, null, "5.1.4-preview.1")]
        [InlineData(0, false, null)]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(1, false, "5.1.3-preview.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        [InlineData(2, null, "6.1.1")]   // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2, false, "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseToLatestPatch_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.1")]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(1, false, "5.1.3-preview.1")]   // Rolls to nearest higher even on patches, but not to latest patch.
        [InlineData(2, null, "6.1.1")]   // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2, false, "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseOnMinor_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, false, null)]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, null)]
        [InlineData(1, false, null)]
        [InlineData(2, null, "7.1.2-preview.1")]
        [InlineData(2, false, "7.1.1-preview.1")]    // Rolls to nearest higher even on patches, but not to latest patch.
        public void RollForwardToPreReleaseOnMajor_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "6.2.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory] // Both 5.2.3-preview.1 and 5.2.3-preview.2 are available
        [InlineData(null, null, "5.2.3-preview.2")]     // Rolls to latest patch - including latest pre-release
        [InlineData(null, false, "5.2.3-preview.1")]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0, null, "5.2.3-preview.2")]
        [InlineData(0, false, null)]
        [InlineData(1, null, "5.2.3-preview.2")]   // Rolls to latest patch - including latest pre-release
        [InlineData(1, false, "5.2.3-preview.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        [InlineData(2, null, "6.1.1")]   // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2, false, "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseToClosestPreRelease_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.2.2"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory] // Both 2.3.1-preview.1 and 2.3.2 are available
        [InlineData(null, null, "2.3.2")]
        [InlineData(null, false, "2.3.2")]
        [InlineData(0, null, "2.3.2")]  // Pre-release is ignored, roll forward to latest release patch
        [InlineData(0, false, null)]    // No exact match available
        [InlineData(1, null, "2.3.2")]
        [InlineData(1, false, "2.3.2")] // Pre-release is ignored, roll forward to closest release available
        [InlineData(2, null, "2.3.2")]
        [InlineData(2, false, "2.3.2")] // Pre-release is ignored, roll forward to closest release available
        public void RollForwardToClosestReleaseWithPreReleaseAvailable_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "2.3.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        // 3.0 change:
        // 2.* - Pre-release will only match the extact x.y.z version, regardless of settings
        // 3.* - Pre-release uses normal roll forward rules, including rolling over minor/patches and obeying settings.
        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(0, false, null)]  // Roll-forward fully disabled
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(2, null, "5.1.4-preview.1")]
        public void RollForwardToPreRelease_FromDifferentPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1-preview.1"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        // 3.0 change:
        // 2.* - Pre-release with exact match will not try to roll forward at all
        // 3.* - Pre-release uses normal roll forward rules, it will roll forward on patches even on exact match.
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.2")]
        [InlineData(0, false, "5.1.3-preview.2")]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(2, null, "5.1.4-preview.1")]
        public void RollForwardToPreRelease_ExactPreReleaseMatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.1"),
                commandResult =>
                    commandResult.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion));
        }

        // 3.0 change:
        // 2.* - Pre-release will select the closest higher version (5.1.3-preview.2 is available in this test, but 5.1.3-preview.1 will be selected over it)
        // 3.* - Pre-release applies roll forward on patches if enabled, always selecting the latest patch version.
        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.2")]
        [InlineData(0, false, "5.1.3-preview.2")]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(2, null, "5.1.4-preview.1")]
        public void RollForwardToPreRelease_FromSamePreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.0"),
                commandResult =>
                    commandResult.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion));
        }

        [Theory]  // When rolling from release, pre-release is ignored if any release which matches can be found
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(2)]
        public void RollForwardToLatestPatch_WithHigherPreReleasePresent(int? rollForwardOnNoCandidateFx)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithFramework(MicrosoftNETCoreApp, "6.1.0"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "6.1.1"));
        }



        private void RunTestWithManyVersions(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithManyVersions, runtimeConfig, resultAction);
        }
        #endregion

        private void RunTest(
            DotNetCli dotNet,
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(
                dotNet,
                SharedState.FrameworkReferenceApp,
                runtimeConfig,
                resultAction);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithOneFramework { get; }

            public DotNetCli DotNetWithPreReleaseFramework { get; }

            public DotNetCli DotNetWithManyVersions { get; }

            public SharedTestState()
            {
                DotNetWithOneFramework = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .Build();

                DotNetWithPreReleaseFramework = DotNet("WithPreReleaseFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.2")
                    .Build();

                DotNetWithManyVersions = DotNet("WithManyVersions")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.3.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.3.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.2.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.4-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.3-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.2-preview.1")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
