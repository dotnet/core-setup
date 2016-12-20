The build system allows flexability around what sort of license is included with
our different artifacts.

We have two classes of artifacts. "Zips" and "Installers". "Zips" include both
the .zip and .tar.gz files produced by the build which include different parts
of .NET Core.  By default, these are licensed using the MIT license.

"Installers" are our MSI/PKG and Linux packages (e.g debs, rpms). These packages
default to using the Microsoft .NET Library software license.

The Environment Variables:

 - `InstallerLicenseType`
 - `ZipFileLicenseType`
 - `NuPkgLicenseType`

Can be used to override the license that is used when building artifacts. The
legal values are `mit` and `msft` which are case sensitive. During the build,
the build scripts use these values to select assets under
`<repo-root>/resources/<license-type>`.

To add an additional license type, clone one of the exisiting directories and
update the resulting resources as you please.
