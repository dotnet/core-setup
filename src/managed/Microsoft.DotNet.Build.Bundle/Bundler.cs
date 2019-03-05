using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    public class Bundler
    {
        string HostName;
        string ContentDir;
        string OutputDir;
        bool EmbedPDBs;

        string TheApp;
        string DepsJson;
        string RuntimeConfigJson;

        // Align embedded assemblies such that they can be loaded 
        // directly from memory-mapped bundle.
        // TBD: Set the correct value of alignment while working on 
        // the runtime changes to load the embedded assemblies.
        const int AssemblyAlignment = 16;

        public static string Version => (BundleManifest.MajorVersion + "." + BundleManifest.MinorVersion);

        public Bundler(string hostName, string contentDir, string outputDir, bool embedPDBs)
        {
            ContentDir = contentDir;
            OutputDir = outputDir;
            HostName = hostName;
            EmbedPDBs = embedPDBs;
        }

        void ValidateFiles()
        {
            // Check required directories
            if (!Directory.Exists(ContentDir))
                throw new BundleException("Dirctory not found: " + ContentDir);
            if (!Directory.Exists(OutputDir))
                throw new BundleException("Dirctory not found: " + OutputDir);

            // Set default names
            string baseName = Path.GetFileNameWithoutExtension(HostName);
            TheApp = baseName + ".dll";
            DepsJson = baseName + ".deps.json";
            RuntimeConfigJson = baseName + ".runtimeconfig.json";

            // Check that required files exist on disk.
            Action<string> checkFileExists = (string name) =>
            {
                string path = Path.Combine(ContentDir, name);
                if (!File.Exists(path))
                    throw new BundleException("File not found: " + path);
            };

            checkFileExists(HostName);
            checkFileExists(TheApp);
            // The *.json files may or may not exist.
        }

        // Embed 'file' into 'singleFile'
        // Returns the offset of the start 'file' within 'singleFile'
        long AddToBundle(Stream singleFile, Stream file, FileType type = FileType.Other)
        {
            // Allign assemblies, since they are loaded directly from bundle
            if (type == FileType.Assembly)
            {
                long padding = AssemblyAlignment - (singleFile.Position % AssemblyAlignment);
                singleFile.Position += padding;
            }

            file.Position = 0;
            long startOffset = singleFile.Position;
            file.CopyTo(singleFile);

            return startOffset;
        }

        FileType InferType(string fileName, Stream file)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension.Equals(".pdb"))
                return FileType.PDB;

            if (fileName.Equals(DepsJson))
                return FileType.DepsJson;

            if (fileName.Equals(RuntimeConfigJson))
                return FileType.RuntimeConfigJson;

            if (fileName.Equals(TheApp))
                return FileType.Application;

            try {
                PEReader peReader = new PEReader(file);
                CorHeader corHeader = peReader.PEHeaders.CorHeader;
                if ((corHeader != null) && ((corHeader.Flags & CorFlags.ILOnly) != 0))
                    return FileType.Assembly;
            }
            catch (BadImageFormatException)
            {
            }

            return FileType.Other;
        }

        void GenerateBundle()
        {
            string singleFilePath = Path.Combine(OutputDir, HostName);

            if (File.Exists(singleFilePath))
                UI.Log($"Ovewriting existing File {singleFilePath}");

            // Start with a copy of the host executable.
            // Copy the file to preserve its permissions.
            File.Copy(Path.Combine(ContentDir, HostName), singleFilePath, overwrite: true);

            using (BinaryWriter oneFile = new BinaryWriter(File.OpenWrite(singleFilePath)))
            {
                Stream singleFile = oneFile.BaseStream;
                BundleManifest manifest = new BundleManifest();

                singleFile.Position = singleFile.Length;
                foreach (string filePath in Directory.GetFiles(ContentDir))
                {
                    string fileName = Path.GetFileName(filePath);

                    // Skip over the host, which is written first.
                    if (fileName.Equals(HostName))
                        continue;

                    using (FileStream file = File.OpenRead(filePath))
                    {
                        FileType type = InferType(fileName, file);

                        // Should this be based on checking the file format, rather than the file name? 
                        if (!EmbedPDBs && type == FileType.PDB)
                        {
                            UI.Log($"Skip [PDB] {fileName}");
                            continue;
                        }

                        long startOffset = AddToBundle(singleFile, file, type);
                        FileEntry entry = manifest.AddEntry(type, fileName, startOffset, file.Length);
                        UI.Log($"Embed: {entry}");
                    }
                }

                manifest.Write(oneFile);
                UI.Log($"SingleFile: Path={singleFilePath} Size={singleFile.Length}");
            }
        }

        public void MakeBundle()
        {
            ValidateFiles();
            GenerateBundle();
        }
    }
}

