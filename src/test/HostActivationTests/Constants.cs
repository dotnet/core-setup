﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    internal static class Constants
    {
        public static class ApplyPatchesSetting
        {
            public const string RuntimeConfigPropertyName = "applyPatches";
        }

        public static class RollForwardOnNoCandidateFxSetting
        {
            public const string RuntimeConfigPropertyName = "rollForwardOnNoCandidateFx";
            public const string CommandLineArgument = "--roll-forward-on-no-candidate-fx";
            public const string EnvironmentVariable = "DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX";
        }

        public static class RollForwardSetting
        {
            public const string RuntimeConfigPropertyName = "rollForward";
            public const string CommandLineArgument = "--roll-forward";
            public const string EnvironmentVariable = "DOTNET_ROLL_FORWARD";

            public const string LatestPatch = "LatestPatch";
            public const string Minor = "Minor";
            public const string Major = "Major";
            public const string LatestMinor = "LatestMinor";
            public const string LatestMajor = "LatestMajor";
            public const string Disable = "Disable";
        }

        public static class FxVersion
        {
            public const string CommandLineArgument = "--fx-version";
        }

        public static class TestOnlyEnvironmentVariables
        {
            public const string RegistryPath = "_DOTNET_TEST_REGISTRY_PATH";
            public const string GloballyRegisteredPath = "_DOTNET_TEST_GLOBALLY_REGISTERED_PATH";
        }
    }
}
