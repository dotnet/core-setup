using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class DependencyVersions
    {
        // TODO: Update these for consuming servicing version of CoreCLR packages
        //       Also, update CoreCLR package referenced at /Users/gkhanna/Github/gkhanna79/core-setup/pkg/projects/Microsoft.NETCore.App/project.json
        public static readonly string CoreCLRVersion = "1.0.4";
        public static readonly string JitVersion = "1.0.4";
    }
}
