// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;

namespace Msi.Tests
{
    public class MsiManager
    {
        private string _msiFile;
        private string _productCode;
        private InstallPackage _installPackage;

        public ProductInstallation Installation
        {
            get
            {
                return ProductInstallation.AllProducts.SingleOrDefault(p => p.ProductCode == _productCode);
            }
        }

        public string InstallLocation
        {
            get
            {
                return IsInstalled ? Installation.InstallLocation : null;
            }
        }

        public bool IsInstalled
        {
            get
            {
                var prodInstall = Installation;
                return Installation == null ? false : prodInstall.IsInstalled;
            }
        }

        public MsiManager(string msiFile)
        {
            _msiFile = msiFile;

            var ispackage = Installer.VerifyPackage(msiFile);
            if (!ispackage)
            {
                throw new ArgumentException("Not a valid MSI file", msiFile);
            }

            _installPackage = new InstallPackage(msiFile, DatabaseOpenMode.ReadOnly);
            _productCode = _installPackage.Property["ProductCode"];
        }

    }
}
