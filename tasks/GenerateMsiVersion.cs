// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;

namespace Microsoft.DotNet.Build.Tasks
{
    // MSI versioning
    // Encode the CLI version to fit into the MSI versioning scheme - https://msdn.microsoft.com/en-us/library/windows/desktop/aa370859(v=vs.85).aspx
    // MSI versions are 3 part
    //                           major.minor.build
    // Size(bits) of each part     8     8    16
    // So we have 32 bits to encode the CLI version
    // Starting with most significant bit this how the CLI version is going to be encoded as MSI Version
    // CLI major  -> 6 bits
    // CLI minor  -> 6 bits
    // CLI patch  -> 6 bits
    // CLI commitcount -> 14 bits
    public class GenerateMsiVersion : BuildTask
    {
        [Required]
        public string Major { get; set; }
        [Required]
        public string Minor { get; set; }
        [Required]
        public string Patch { get; set; }
        [Required]
        public string BuildNumber { get; set; }
        [Output]
        public string MsiVersion { get; set; }

        public override bool Execute()
        {
            var major = int.Parse(Major) << 26;
            var minor = int.Parse(Minor) << 20;
            var patch = int.Parse(Patch) << 14;
            var msiVersionNumber = major | minor | patch | int.Parse(BuildNumber);

            var msiMajor = (msiVersionNumber >> 24) & 0xFF;
            var msiMinor = (msiVersionNumber >> 16) & 0xFF;
            var msiBuild = msiVersionNumber & 0xFFFF;

            MsiVersion = $"{msiMajor}.{msiMinor}.{msiBuild}";

            return true;
        }
    }
}
