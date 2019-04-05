﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardOnNoCandidateFxMultipleFrameworks :
        FrameworkResolutionBase,
        IClassFixture<RollForwardOnNoCandidateFxMultipleFrameworks.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";
        private const string AnotherMiddleWare = "AnotherMiddleWare";
        private const string HighWare = "HighWare";

        private SharedTestState SharedState { get; }

        public RollForwardOnNoCandidateFxMultipleFrameworks(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Soft roll forward from the inner framework reference [specified] to app's 5.1.1 (defaults)
        [Theory]
        [InlineData("5.0.0", 0,    null,  null)]
        [InlineData("5.1.0", 0,    null,  "5.1.3")]
        [InlineData("5.1.0", 0,    false, null)]
        [InlineData("5.1.1", 0,    false, "5.1.1")]
        [InlineData("5.0.0", null, null,  "5.1.3")]
        [InlineData("5.0.0", 1,    null,  "5.1.3")]
        [InlineData("5.1.0", 1,    false, "5.1.1")]
        [InlineData("1.0.0", 1,    null,  null)]
        [InlineData("1.0.0", 2,    null,  "5.1.3")]
        public void SoftRollForward_InnerFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1"));
        }

        // Soft roll forward from the inner framework reference [specified] to app's 5.1.1 (defaults)
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference .
        [Theory]
        [InlineData("5.0.0", 0, null, null)]
        [InlineData("5.1.0", 0, null, "5.1.3")]
        [InlineData("5.1.0", 0, false, null)]
        [InlineData("5.1.3", 0, false, "5.1.3")]
        [InlineData("5.0.0", null, null, "5.1.3")]
        [InlineData("5.0.0", 1, null, "5.1.3")]
        [InlineData("5.0.0", 1, false, "5.1.1")]
        [InlineData("5.1.0", 1, false, "5.1.1")]
        [InlineData("1.0.0", 1, null, null)]
        [InlineData("1.0.0", 2, null, "5.1.3")]
        public void SoftRollForward_InnerFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1"));
        }

        // Soft roll forward from inner framework reference [specified] to  app's 5.1.1 (defaults)
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference));
        }

        // Soft roll forward from inner framework reference [specified] to  app's 5.1.1 (defaults)
        // In this case the app reference to core framework comes first, which means it's going to be hard resolved
        // and only then the soft roll forward to the inner reference is performed. So the hard resolved version
        // is use in the soft roll forward.
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerFrameworkReference_ToLower_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference));
        }

        // Soft roll forward from inner framework reference [specified] to app's 6.1.1-preview.0 (defaults)
        [Theory]
        // 3.0 change:
        // 2.* - release would never roll forward to pre-release
        // 3.* - release rolls forward to pre-release if there is no available release match
        [InlineData("6.0.0", null, null, "6.1.1-preview.1")]
        [InlineData("6.0.1-preview.0", null, null, "6.1.1-preview.1")]
        [InlineData("6.1.1-preview.0", null, null, "6.1.1-preview.1")]
        [InlineData("6.0.1-preview.0", 0, null, null)]
        [InlineData("6.1.0-preview.0", 0, false, null)]
        [InlineData("6.1.0-preview.0", 0, null, "6.1.1-preview.1")] // This is effectively a bug, the design was that pre-release should never roll on patches
        [InlineData("6.1.1-preview.0", 0, null, "6.1.1-preview.1")]
        [InlineData("6.1.1-preview.0", 0, false, "6.1.1-preview.1")] // applyPatches=false is ignored for pre-release roll
        [InlineData("6.1.1-preview.1", 0, null, "6.1.1-preview.1")]
        public void SoftRollForward_InnerFrameworkReference_PreRelease(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.1.1-preview.0")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "6.1.1-preview.0"));
        }

        // Soft roll forward from inner framework reference 5.1.1 to app [specified version]
        [Theory]
        [InlineData("5.0.0", 0, null, null)]
        [InlineData("5.1.0", 0, null, "5.1.3")]
        [InlineData("5.1.0", 0, false, null)]
        [InlineData("5.1.1", 0, false, "5.1.1")]
        [InlineData("5.0.0", null, null, "5.1.3")]
        [InlineData("5.1.0", 1, null, "5.1.3")]
        [InlineData("5.1.0", 1, false, "5.1.1")]
        [InlineData("5.0.0", 1, null, "5.1.3")]
        [InlineData("1.0.0", 1, null, null)]
        [InlineData("1.0.0", 2, null, "5.1.3")]
        public void SoftRollForward_AppFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1"));
        }

        // Soft roll forward from app [specified version] to inner framework reference 5.1.1
        [Theory]
        [InlineData("5.0.0", 0, null, null)]
        [InlineData("5.1.0", 0, null, "5.1.3")]
        [InlineData("5.1.0", 0, false, null)]
        [InlineData("5.1.1", 0, false, "5.1.1")]
        [InlineData("5.0.0", null, null, "5.1.3")]
        [InlineData("5.1.0", 1, null, "5.1.3")]
        [InlineData("5.1.0", 1, false, "5.1.1")]
        [InlineData("5.0.0", 1, null, "5.1.3")]
        [InlineData("1.0.0", 1, null, null)]
        [InlineData("1.0.0", 2, null, "5.1.3")]
        public void SoftRollForward_AppFrameworkReference_ToLower_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.DidNotFindCompatibleFrameworkVersion());
        }

        // Soft roll forward from inner framework reference 5.1.1 to app [specified version]
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_AppFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference));
        }

        // Soft roll forward from inner framework reference 5.1.1 to app [specified version]
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_AppFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference));
        }

        // Soft roll forward inner framework reference (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.0.0", 0,    null,  null)]
        [InlineData("5.1.0", 0,    null,  "5.1.3")]
        [InlineData("5.1.0", 0,    false, null)]
        [InlineData("5.0.0", null, null,  "5.1.3")]
        [InlineData("5.0.0", 1,    null,  "5.1.3")]
        [InlineData("1.0.0", 1,    null,  null)]
        [InlineData("1.0.0", 2,    null,  "5.1.3")]
        public void SoftRollForward_InnerToInnerFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
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
                            .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                            .WithApplyPatches(applyPatches)
                            .Version = versionReference);
                },
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1"));
        }

        // Soft roll forward inner framework reference (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerToInnerFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
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
                            .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                            .WithApplyPatches(applyPatches)
                            .Version = versionReference);
                },
                resolvedFramework,
                commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference));
        }

        // This test does:
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
                    .WithRollForwardOnNoCandidateFx(2)
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
                },
                resultValidator: commandResult =>
                    commandResult.Should().Pass()
                        .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                        .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0"));
        }

        // This test does:
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
                    .WithRollForwardOnNoCandidateFx(2)
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
                },
                resultValidator: commandResult =>
                    commandResult.Should().Pass()
                        .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                        .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0"));
        }

        [Fact]
        public void RollForwardOnAllFrameworks()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.0.0")
                    .WithFramework(HighWare, "7.0.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.0.0");
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                    {
                        runtimeConfig.GetFramework(MiddleWare)
                            .Version = "2.0.0";
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.0.0";
                    });
                },
                resultValidator: commandResult =>
                    commandResult.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3")
                        .And.HaveResolvedFramework(MiddleWare, "2.1.2")
                        .And.HaveResolvedFramework(HighWare, "7.3.1"));
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            string resolvedFramework = null,
            Action<CommandResult> resultValidator = null)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = SharedState.DotNetWithMultipleFrameworks.Customize())
            {
                customizeDotNet?.Invoke(dotnetCustomizer);

                RunTest(
                    SharedState.DotNetWithMultipleFrameworks,
                    SharedState.FrameworkReferenceApp,
                    new TestSettings().WithRuntimeConfigCustomizer(runtimeConfig),
                    commandResult =>
                    {
                        if (resolvedFramework != null)
                        {
                            commandResult.Should().Pass()
                                .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
                        }
                        else
                        {
                            resultValidator?.Invoke(commandResult);
                        }
                    });
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithMultipleFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithMultipleFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.4.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.6.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.0.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.1-preview.1")
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
    }
}
