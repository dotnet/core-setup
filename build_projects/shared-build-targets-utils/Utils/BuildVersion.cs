using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class BuildVersion : Version
    {
        public string SimpleVersion => $"{Major}.{Minor}.{Patch}.{CommitCountString}";
        public string VersionSuffix => $"{ReleaseSuffix}-{CommitCountString}";
        
        // Uncomment this for stabilization build
        public string NetCoreAppVersion => $"{ProductionVersion}";

        // Uncomment this for pre-release builds
        // public string NetCoreAppVersion => $"{ProductionVersion}-{VersionSuffix}-00";
        public string ProductionVersion => $"{Major}.{Minor}.{Patch}";
    }
}
