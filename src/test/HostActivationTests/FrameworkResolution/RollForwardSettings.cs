// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardSettings :
        FrameworkResolutionBase,
        IClassFixture<RollForwardSettings.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";

        private SharedTestState SharedState { get; }

        public RollForwardSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Fact]
        public void Default()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Theory]
        [InlineData(SettingLocation.CommandLine)]
        [InlineData(SettingLocation.Environment)]
        [InlineData(SettingLocation.RuntimeOptions)]
        [InlineData(SettingLocation.FrameworkReference)]
        public void InvalidValue(SettingLocation settingLocation)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardSetting(settingLocation, "InvalidValue")),
                result => result.Should().Fail()
                    .And.DidNotRecognizeRollForwardValue("InvalidValue"));
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_CommandLine(string rollForward)
        {
            ValueIgnoresCase(SettingLocation.CommandLine, rollForward);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_Environment(string rollForward)
        {
            ValueIgnoresCase(SettingLocation.Environment, rollForward);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_RuntimeOptions(string rollForward)
        {
            ValueIgnoresCase(SettingLocation.RuntimeOptions, rollForward);
        }

        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_FrameworkReference(string rollForward)
        {
            ValueIgnoresCase(SettingLocation.FrameworkReference, rollForward);
        }

        private void ValueIgnoresCase(SettingLocation settingLocation, string rollForward)
        {
            string[] values = new string[]
            {
                rollForward,
                rollForward.ToLowerInvariant(),
                rollForward.ToUpperInvariant()
            };

            foreach (string value in values)
            {
                RunTest(
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                        .With(RollForwardSetting(settingLocation, value)),
                    result => result.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
            }
        }

        [Fact]
        public void CollisionsOnCommandLine()
        {
            RunTest(
                runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Fail()
                    .And.HaveStdErrContaining(
                        $"It's invalid to use both '{Constants.RollForwardSetting.CommandLineArgument}' and " +
                        $"'{Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument}' command line options."),
                commandLine: new string[] {
                    Constants.RollForwardSetting.CommandLineArgument, Constants.RollForwardSetting.LatestPatch,
                    Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, "2"
                });
        }

        [Theory]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.None, SettingLocation.None, true)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.RuntimeOptions, SettingLocation.None, false)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.FrameworkReference, SettingLocation.None, false)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.None, SettingLocation.RuntimeOptions, false)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.None, SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.RuntimeOptions, SettingLocation.RuntimeOptions, false)]
        [InlineData(SettingLocation.RuntimeOptions, SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None, SettingLocation.None, true)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.RuntimeOptions, SettingLocation.None, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, SettingLocation.None, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None, SettingLocation.RuntimeOptions, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None, SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.RuntimeOptions, SettingLocation.RuntimeOptions, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, false)]
        public void CollisionsInRuntimeConfig(
            SettingLocation rollForwardLocation,
            SettingLocation rollForwardOnNoCandidateFxLocation,
            SettingLocation applyPatchesLocation,
            bool passes)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                    .With(RollForwardSetting(rollForwardLocation, Constants.RollForwardSetting.Minor))
                    .With(RollForwardOnNoCandidateFxSetting(rollForwardOnNoCandidateFxLocation, 1))
                    .With(ApplyPatchesSetting(applyPatchesLocation, false)),
                result =>
                {
                    if (passes)
                    {
                        result.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
                    }
                    else
                    {
                        result.Should().Fail()
                        .And.HaveStdErrContaining(
                            $"It's invalid to use both `{Constants.RollForwardSetting.RuntimeConfigPropertyName}` and one of " +
                            $"`{Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName}` or " +
                            $"`{Constants.ApplyPatchesSetting.RuntimeConfigPropertyName}` in the same runtime config.");
                    }
                });
        }

        [Fact]
        public void RuntimeConfigOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForward(Constants.RollForwardSetting.Major)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction,
            string[] commandLine = null)
        {
            RunTest(
                SharedState.DotNetWithFrameworks,
                SharedState.FrameworkReferenceApp,
                runtimeConfig,
                resultAction,
                commandLine: commandLine);
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
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddFramework(
                        MiddleWare, "2.1.2",
                        runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
