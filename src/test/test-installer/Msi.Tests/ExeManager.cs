// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Msi.Tests
{
    public class ExeManager
    {
        private string _bundleFile;

        public ExeManager(string exeFile)
        {
            _bundleFile = exeFile;
        }

        public bool Install(string customLocation = null)
        {
            string dotnetHome = "";
            if (!string.IsNullOrEmpty(customLocation))
            {
                dotnetHome = $"DOTNETHOME={customLocation}";
            }

            RunBundle(dotnetHome);

            return true;
        }

        public bool UnInstall()
        {
            RunBundle("/uninstall");

            return true;
        }

        private void RunBundle(string additionalArguments)
        {
            var arguments = $"/q /norestart {additionalArguments}";
            var process = Process.Start(_bundleFile, arguments);

            if (!process.WaitForExit(5 * 60 * 1000))
            {
                throw new InvalidOperationException($"Failed to wait for the installation operation to complete. Check to see if the installation process is still running. Command line: {_bundleFile} {arguments}");
            }

            else if (0 != process.ExitCode)
            {
                throw new InvalidOperationException($"The installation operation failed with exit code: {process.ExitCode}. Command line: {_bundleFile} {arguments}");
            }
        }
    }
}
