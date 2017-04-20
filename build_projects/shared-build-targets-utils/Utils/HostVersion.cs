using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class HostVersion : Version
    {
        // ------------------------------------------HOST-VERSIONING-------------------------------------------
        //
        // Host versions are independent of CLI versions. Moreover, these version numbers
        // are baked into the binary and is used to look up a serviced binary replacement.
        //

        public struct VerInfo
        {
            public int Major;
            public int Minor;
            public int Patch;
            public string Release;
            public string BuildMajor;
            public string BuildMinor;
            public string CommitCountString;

            public VerInfo(int major, int minor, int patch, string release, string buildMajor, string buildMinor, string commitCountString)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
                Release = release;
                BuildMajor = buildMajor;
                BuildMinor = buildMinor;
                CommitCountString = commitCountString;
            }

            public string GenerateMsiVersion()
            {
                return Version.GenerateMsiVersion(Major, Minor, Patch, Int32.Parse(VerRsrcBuildMajor));
            }

            public string WithoutSuffix => $"{Major}.{Minor}.{Patch}";

            // The version numbers to be included in the embedded version resource (.rc) files.
            public string VerRsrcBuildMajor => !string.IsNullOrEmpty(BuildMajor) ? BuildMajor : CommitCountString;
            public string VerRsrcBuildMinor => !string.IsNullOrEmpty(BuildMinor) ? BuildMinor : "00";

            public override string ToString()
            {
                string suffix = "";
                foreach (var verPad in new string[] { Release, BuildMajor, BuildMinor })
                {
                    if (!string.IsNullOrEmpty(verPad))
                    {
                        suffix += $"-{verPad}";
                    }
                }
                return $"{Major}.{Minor}.{Patch}{suffix}";
            }
        }
        //
        // Latest hosts for production of nupkgs.
        //

        // Full versions and package information.
        public string LatestHostBuildMajor => CommitCountString;
        public string LatestHostBuildMinor => "00";
        public bool EnsureStableVersion => true;

        // Comment below lines when stabilizing 1.1.X and we are going to update one (or more) of the host packages.
        //
        // public VerInfo LatestHostVersion => new VerInfo(1, 1, 1, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        // public VerInfo LatestHostFxrVersion => new VerInfo(1, 1, 1, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        // public VerInfo LatestHostPolicyVersion => new VerInfo(1, 1, 1, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);

        // These are the versions used by GenerateMSbuildPropsFile to generate version.props that is used for
        // versioning of the host packages.
        //
        // These should only be incremented in a servicing release if we are updating one (or more) of the host packages.

        public VerInfo LatestHostVersion => new VerInfo(1, 1, 0, "", "", "", CommitCountString);
        public VerInfo LatestHostFxrVersion => new VerInfo(1, 1, 0, "", "", "", CommitCountString);
        public VerInfo LatestHostPolicyVersion => new VerInfo(1, 1, 2, "", "", "", CommitCountString);


        // If you are producing host packages use this to validate them.
        public Dictionary<string, VerInfo> LatestHostPackagesToValidate => new Dictionary<string, VerInfo>()
        {
            // Add packages here to validate that they are produced, similar to LatestHostPackages.
            { "Microsoft.NETCore.DotNetHostPolicy", LatestHostPolicyVersion }
        };
        public Dictionary<string, VerInfo> LatestHostPackages => new Dictionary<string, VerInfo>()
        {
            { "Microsoft.NETCore.DotNetHost", LatestHostVersion },
            { "Microsoft.NETCore.DotNetHostResolver", LatestHostFxrVersion },
            { "Microsoft.NETCore.DotNetHostPolicy", LatestHostPolicyVersion }
        };
        public Dictionary<string, VerInfo> LatestHostBinaries => new Dictionary<string, VerInfo>()
        {
            { "dotnet", LatestHostVersion },
            { "hostfxr", LatestHostFxrVersion },
            { "hostpolicy", LatestHostPolicyVersion }
        };

        //
        // Locked muxer for consumption in CLI.
        //
        // These are used for generating platform installers.
        //
        public bool IsLocked = true; // Set this variable to toggle muxer locking.
        public VerInfo LockedHostFxrVersion => IsLocked ? new VerInfo(1, 1, 0, "", "", "", CommitCountString) : LatestHostFxrVersion;
        public VerInfo LockedHostVersion    => IsLocked ? new VerInfo(1, 1, 0, "", "", "", CommitCountString) : LatestHostVersion;
        public bool fExplicitHostFXRMSIVersion = true; //This should be set to false when we no longer need to override the MSI version to be different from the HostFXR nuget package version".

        // This method returns the locked hostfxr version based on the flag fExplicitHostFXRMSIVersion and the current platform.
        // For MSI (Windows) generation we specify a newer version for handling issue #1574 and for non-Windows platform we return the LockedHostFxrVersion.
        public VerInfo GetLockedHostFXRPlatformInstallerVersion()
        {
            VerInfo version = LockedHostFxrVersion;

            if (fExplicitHostFXRMSIVersion && CurrentPlatform.Current == BuildPlatform.Windows)
            {
                version = new VerInfo(1, 1, 2, "", "", "", CommitCountString);
            }

            return version;
        }
    }
}
