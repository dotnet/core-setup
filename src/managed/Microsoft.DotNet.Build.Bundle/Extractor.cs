using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    public class Extractor
    {
        string OutputDir;
        string SingleFilePath;

        public Extractor(string singleFilePath, string outputDir)
        {
            SingleFilePath = singleFilePath;
            OutputDir = outputDir;
        }

        public void Spill()
        {
            try
            {
                if (!File.Exists(SingleFilePath))
                    throw new BundleException("File not found: " + SingleFilePath);

                using (BinaryReader oneFile = new BinaryReader(File.OpenRead(SingleFilePath)))
                {
                    BundleManifest manifest = new BundleManifest();
                    manifest.Read(oneFile);

                    foreach (FileEntry entry in manifest.Files)
                    {
                        UI.Log($"Spill: {entry}");
                        string filePath = Path.Combine(OutputDir, entry.Name);
                        oneFile.BaseStream.Position = entry.Offset;
                        using (BinaryWriter file = new BinaryWriter(File.Create(filePath)))
                        {
                            long size = entry.Size;
                            do
                            {
                                int copySize = (int)(size % int.MaxValue);
                                file.Write(oneFile.ReadBytes(copySize));
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

