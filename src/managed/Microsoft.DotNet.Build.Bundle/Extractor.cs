// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    /// Extractor: The functionality to extract the files embedded 
    /// within a bundle to sepearte files.
    /// </summary>

    public class Extractor
    {
        string OutputDir;
        string BundlePath;

        public Extractor(string bundlePath, string outputDir)
        {
            BundlePath = bundlePath;
            OutputDir = outputDir;
        }

        public void Spill()
        {
            try
            {
                if (!File.Exists(BundlePath))
                    throw new BundleException("File not found: " + BundlePath);

                using (BinaryReader reader = new BinaryReader(File.OpenRead(BundlePath)))
                {
                    BundleManifest manifest = new BundleManifest();
                    manifest.Read(reader);

                    foreach (FileEntry entry in manifest.Files)
                    {
                        Program.Log($"Spill: {entry}");
                        string filePath = Path.Combine(OutputDir, entry.Name);
                        reader.BaseStream.Position = entry.Offset;
                        using (BinaryWriter file = new BinaryWriter(File.Create(filePath)))
                        {
                            long size = entry.Size;
                            do
                            {
                                int copySize = (int)(size % int.MaxValue);
                                file.Write(reader.ReadBytes(copySize));
                                size -= copySize;
                            } while (size > 0);
                        }
                    }
                }
            }
            catch (IOException)
            {
                throw new BundleException("Malformed Bundle");
            }
        }
    }
}

