// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public partial class FrameworkResolutionBase
    {
        public enum SettingLocation
        {
            CommandLine,
            Environment,
            RuntimeOptions,
            FrameworkReference
        }

        public static Func<TestSettings, TestSettings> RollForwardSetting(
            SettingLocation location,
            string value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (value == null)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.Environment:
                    return testSettings => testSettings.WithEnvironment(Constants.RollForwardSetting.EnvironmentVariable, value);
                case SettingLocation.CommandLine:
                    return testSettings => testSettings.WithCommandLine(Constants.RollForwardSetting.CommandLineArgument, value);
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithRollForward(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithRollForward(value);
                        return rc;
                    });
                default:
                    throw new Exception($"RollForward forward doesn't support setting location {location}.");
            }
        }

        public static Func<TestSettings, TestSettings> RollForwardOnNoCandidateFxSetting(
            SettingLocation location,
            int? value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (!value.HasValue)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.Environment:
                    return testSettings => testSettings.WithEnvironment(Constants.RollFowardOnNoCandidateFxSetting.EnvironmentVariable, value.ToString());
                case SettingLocation.CommandLine:
                    return testSettings => testSettings.WithCommandLine(Constants.RollFowardOnNoCandidateFxSetting.CommandLineArgument, value.ToString());
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithRollForwardOnNoCandidateFx(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithRollForwardOnNoCandidateFx(value);
                        return rc;
                    });
                default:
                    throw new Exception($"RollFowardOnNoCandidateFx doesn't support setting location {location}.");
            }
        }

        public static Func<TestSettings, TestSettings> ApplyPatchesSetting(
            SettingLocation location,
            bool? value,
            string frameworkReferenceName = MicrosoftNETCoreApp)
        {
            if (!value.HasValue)
            {
                return testSettings => testSettings;
            }

            switch (location)
            {
                case SettingLocation.RuntimeOptions:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc => rc.WithApplyPatches(value));
                case SettingLocation.FrameworkReference:
                    return testSettings => testSettings.WithRuntimeConfigCustomizer(rc =>
                    {
                        rc.GetFramework(frameworkReferenceName).WithApplyPatches(value);
                        return rc;
                    });
                default:
                    throw new Exception($"ApplyPatches doesn't support setting location {location}.");
            }
        }
    }
}
