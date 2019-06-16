// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Embeds the App Name into the AppHost.exe
    /// If an apphost is a single-file bundle, updates the location of the bundle headers.
    /// </summary>
    public static class AppUpdater
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        private const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        private readonly static byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        private const string BundleHeaderPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f3";
        private readonly static byte[] BundleHeaderPlaceholderSearchValue = Encoding.UTF8.GetBytes(BundleHeaderPlaceholder);

        /// <summary>
        /// Create an AppHost with embedded configuration of app binary location
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="appBinaryFilePath">Full path to app binary or relative path to the result apphost file</param>
        /// <param name="windowsGraphicalUserInterface">Specify whether to set the subsystem to GUI. Only valid for PE apphosts.</param>
        /// <param name="intermediateAssembly">Path to the intermediate assembly, used for copying resources to PE apphosts.</param>
        public static void UpdateAppPath(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            string appBinaryFilePath,
            bool windowsGraphicalUserInterface = false,
            string intermediateAssembly = null)
        {
            var bytesToWrite = Encoding.UTF8.GetBytes(appBinaryFilePath);
            if (bytesToWrite.Length > 1024)
            {
                throw new BinaryUpdateException($"Given file name {appBinaryFilePath} is longer than 1024 bytes");
            }

            var destinationDirectory = new FileInfo(appHostDestinationFilePath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy apphost to destination path so it inherits the same attributes/permissions.
            File.Copy(appHostSourceFilePath, appHostDestinationFilePath, overwrite: true);

            // Re-write the destination apphost with the proper contents.
            bool appHostIsPEImage = false;
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationFilePath))
            {
                using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                {
                    if(!BinaryUtils.SearchAndReplace(accessor, AppBinaryPathPlaceholderSearchValue, bytesToWrite))
                    {
                        throw new BinaryUpdateException($"Unable to use '{appHostSourceFilePath}' as application host executable as it does not contain the expected placeholder byte sequence '{AppBinaryPathPlaceholder}' that would mark where the application name would be written");
                    }

                    appHostIsPEImage = BinaryUtils.IsPEImage(accessor);

                    if (windowsGraphicalUserInterface)
                    {
                        if (!appHostIsPEImage)
                        {
                            throw new BinaryUpdateException($"Unable to use '{appHostSourceFilePath}' as application host executable because it's not a Windows executable for the CUI (Console) subsystem");
                        }

                        BinaryUtils.SetWindowsGraphicalUserInterfaceBit(accessor, appHostSourceFilePath);
                    }
                }
            }

            if (intermediateAssembly != null && appHostIsPEImage)
            {
                if (ResourceUpdater.IsSupportedOS())
                {
                    // Copy resources from managed dll to the apphost
                    new ResourceUpdater(appHostDestinationFilePath)
                        .AddResourcesFromPEImage(intermediateAssembly)
                        .Update();
                }
            }

            // Memory-mapped write does not updating last write time
            File.SetLastWriteTimeUtc(appHostDestinationFilePath, DateTime.UtcNow);
        }

        /// <summary>
        /// Create an AppHost with embedded configuration of app binary location
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="bundleHeaderOffset">The offset to the location of bundle header</param>
        public static void UpdateBundleHeader(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            long bundleHeaderOffset)
        {
            var destinationDirectory = new FileInfo(appHostDestinationFilePath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy apphost to destination path so it inherits the same attributes/permissions.
            File.Copy(appHostSourceFilePath, appHostDestinationFilePath, overwrite: true);

            // Re-write the destination apphost with the proper contents.
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationFilePath))
            {
                using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                {
                    if (!BinaryUtils.SearchAndReplace(accessor, BundleHeaderPlaceholderSearchValue, BitConverter.GetBytes(bundleHeaderOffset)))
                    {
                        throw new BinaryUpdateException($"Unable to use '{appHostSourceFilePath}' as application host executable as it does not contain the expected placeholder byte sequence '{BundleHeaderPlaceholder}' that would mark where the bundle header offset would be written");
                    }
                }
            }

            // Memory-mapped write does not updating last write time
            File.SetLastWriteTimeUtc(appHostDestinationFilePath, DateTime.UtcNow);
        }

    }
}
