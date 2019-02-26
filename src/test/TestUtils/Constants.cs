// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class Constants
    {
        //public static readonly string ProjectFileName = "project.json";
        public static readonly string ExeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        // Priority order of runnable suffixes to look for and run
        public static readonly string[] RunnableSuffixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                         ? new string[] { ".exe", ".cmd", ".bat" }
                                                         : new string[] { string.Empty };
    }
}
