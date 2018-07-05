using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class Monikers
    {
        public const string SharedFrameworkName = "Microsoft.NETCore.App";
        public const string SharedFxBrandName = "Microsoft .NET Core 1.1.10 - Runtime";
        public const string SharedHostBrandName = "Microsoft .NET Core 1.1.10 - Host";
        public const string HostFxrBrandName = "Microsoft .NET Core 1.1.10 - Host FX Resolver";

        public static string GetProductMoniker(BuildTargetContext c, string artifactPrefix, string version)
        {
            string rid = Environment.GetEnvironmentVariable("TARGETRID") ?? RuntimeEnvironment.GetRuntimeIdentifier();

            if (rid == "debian.9-x64" || rid == "ubuntu.16.04-x64" || rid == "ubuntu.16.10-x64" || rid == "ubuntu.18.04-x64" || rid == "fedora.23-x64" || rid == "fedora.24-x64" || rid == "fedora.27-x64" || rid == "fedora.28-x64" || rid == "opensuse.13.2-x64" || rid == "opensuse.42.1-x64" || rid == "opensuse.42.3-x64")
            {
                return $"{artifactPrefix}-{rid}.{version}";
            }
            else
            {
                string osname = GetOSShortName();
                string arch = Environment.GetEnvironmentVariable("TARGETPLATFORM") ?? CurrentArchitecture.Current.ToString();
                return $"{artifactPrefix}-{osname}-{arch}.{version}";
            }
        }

        public static string GetBadgeMoniker()
        {
            switch (RuntimeEnvironment.GetRuntimeIdentifier())
            {
                case "debian.9-x64":
                     return "Debian_9_x64";
                case "ubuntu.16.04-x64":
                    return "Ubuntu_16_04_x64";
                case "ubuntu.16.10-x64":
                     return "Ubuntu_16_10_x64";
                case "ubuntu.18.04-x64":
                     return "Ubuntu_18_04_x64";
                case "fedora.23-x64":
                     return "Fedora_23_x64";
                case "fedora.24-x64":
                     return "Fedora_24_x64";
                case "fedora.27-x64":
                     return "Fedora_27_x64";
                case "fedora.28-x64":
                    return "Fedora_28_x64";
                case "opensuse.13.2-x64":
                     return "openSUSE_13_2_x64";
                case "opensuse.42.1-x64":
                     return "openSUSE_42_1_x64";
                case "opensuse.42.3-x64":
                     return "openSUSE_42_3_x64";
            }

            return $"{CurrentPlatform.Current}_{Environment.GetEnvironmentVariable("TARGETPLATFORM") ?? CurrentArchitecture.Current.ToString()}";
        }

        public static string GetDebianHostFxrPackageName(string hostfxrNugetVersion)
        {
            return $"dotnet-hostfxr-{hostfxrNugetVersion}".ToLower();
        }

        public static string GetDebianSharedFrameworkPackageName(string sharedFrameworkNugetVersion)
        {
            return $"dotnet-sharedframework-{SharedFrameworkName}-{sharedFrameworkNugetVersion}".ToLower();
        }

        public static string GetDebianSharedHostPackageName(BuildTargetContext c)
        {
            return $"dotnet-host".ToLower();
        }

        public static string GetOSShortName()
        {
            string osname = "";
            switch (CurrentPlatform.Current)
            {
                case BuildPlatform.Windows:
                    osname = "win";
                    break;
                default:
                    osname = CurrentPlatform.Current.ToString().ToLower();
                    break;
            }

            return osname;
        }
    }
}
