using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class BuildVersion : Version
    {
        public string SimpleVersion => $"{Major}.{Minor}.{Patch}.{CommitCountString}";
        public string VersionSuffix => $"{ReleaseSuffix}-{CommitCountString}";
        
        // Uncomment below for stabilization
        public string NetCoreAppVersion => $"{Major}.{Minor}.{Patch}";

        // Uncomment below for pre-release build
        // public string NetCoreAppVersion => $"{Major}.{Minor}.{Patch}-{VersionSuffix}-00";
        public string ProductionVersion => $"{Major}.{Minor}.{Patch}";
    }
}
