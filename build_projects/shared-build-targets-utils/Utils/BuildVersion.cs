using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class BuildVersion : Version
    {
        public string SimpleVersion => $"{Major}.{Minor}.{Patch}.{CommitCountString}";
        public string VersionSuffix => $"{ReleaseSuffix}-{CommitCountString}";
        public string NetCoreAppVersion => $"{Major}.{Minor}.{Patch}-{VersionSuffix}-00";
        public string ProductionVersion => $"{Major}.{Minor}.{Patch}";
    }
}
