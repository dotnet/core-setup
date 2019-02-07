using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace SingleFile
{
    public class BundleException : Exception
    {
        public BundleException(string message) :
                base(message)
        {
        }
    }
    public class BundleArgumentException : BundleException
    {
        public BundleArgumentException(string message) :
                base(message)
        {
        }
    }

    // The bundler differentiates a few kinds of files via the manifest,
    // with respect to the way in which they'll be used by the runtime.
    //
    // - Runtime Configuration files: processed directly from memory
    // - Assemblies: loaded directly from memory.
    //               Currently, these are only pure-managed assemblies.
    //               Future versions should include R2R assemblies here.
    // - Other files (simply called "Files" below): files that will be 
    //               extracted out to disk. These include native binaries.

    public enum FileType
    {
        TheApp,            // Represents the main app, also an assembly
        Assembly,          // IL Assemblies, which will be processed from bundle
        DepsJson,          // Configuration file, processed from bundle
        RuntimeConfigJson, // Configuration file, processed from bundle
        PDB,               // PDB file, not bundled by default
        Other              // Files spilled to disk by the host
    };

    // FileEntry: Records information about embedded files.
    // A manifest of embedded files is recorded in the bundle meta-data.
    // For each embedded file, the following information is written:
    //
    // - - - - - - - - - - - - - - - - - - - - - - - -
    // Name| File                   | File   | File
    // Len | Name                   | Offset | Size
    // - - - - - - - - - - - - - - - - - - - - - - - -

    public class FileEntry
    {
        public FileType Type;
        public string Name;
        public long Offset;
        public long Size;

        public FileEntry(FileType fileType, string name, long offset, long size)
        {
            Type = fileType;
            Name = name;
            Offset = offset;
            Size = size;
        }

        public void Write(BinaryWriter oneFile)
        {
            oneFile.Write(Name);
            oneFile.Write(Offset);
            oneFile.Write(Size);
        }

        public static FileEntry Read(BinaryReader oneFile, FileType fileType = FileType.Other)
        {
            string fileName = oneFile.ReadString();
            long offset = oneFile.ReadInt64();
            long size = oneFile.ReadInt64();
            return new FileEntry(fileType, fileName, offset, size);
        }

        public override string ToString()
        {
            return String.Format($"{Name} [{Type}] @{Offset} Sz={Size}");
        }
    }

    // BundleManifest:
    // Here is the description of the Bundle Layout:
    //_______________________________________________
    //  AppHost 
    //
    //
    // ----------------------------------------------
    // The embedded files including the app, its
    // configuration files, dependencies, and 
    // possibly the runtime.
    // 
    // 
    // 
    // 
    // 
    // 
    //
    // -----------------------------------------------
    // Meta-data Directory (FileEntrys)
    //   "app.deps.json"           (if any)
    //   "app.runtimeconfig.json"  (if any)
    //   "app.dll"                 (The main app)
    //   "IL_1.dll"                (Assemblies, if any)
    //    ...
    //   "IL_n.dll"
    //   "File_1"                  (Other Files, if any)
    //    ...
    //   "File_n" 
    // - - - - - - - - - - - - - - - - - - - - - - - -
    //  Bundle Manifest (footer)
    //     MajorVersion
    //     MinorVersion
    //     FileEntryStart (offset)
    //     NumFiles
    //     NumAssemblies
    //     Flags
    // -----------------------------------------------
    //   Bundle Signature
    // _________________________________________________

    [Flags]
    public enum BundleFlags
    {
        None = 0,
        HasDepsJson = 1,
        HasRuntimeConfigJson = 2
    }

    public class BundleManifest
    {
        public const string Signature = ".NetCoreBundle";
        public const uint MajorVersion = 0;
        public const uint MinorVersion = 1;

        const uint SignatureLength = 15;
        const uint FooterLength = 43;

        long FileEntryStart;
        int AssemblyCount;
        BundleFlags flags;

        public List<FileEntry> Files;

        public BundleManifest()
        {
            Files = new List<FileEntry>();
            flags = BundleFlags.None;
        }

        public FileEntry AddEntry(FileType type, string name, long offset, long size)
        {
            switch(type)
            {
                case FileType.DepsJson:
                    flags |= BundleFlags.HasDepsJson;
                    break;
                case FileType.RuntimeConfigJson:
                    flags |= BundleFlags.HasRuntimeConfigJson;
                    break;
                default:
                    break;
            }

            FileEntry entry = new FileEntry(type, name, offset, size);
            Files.Add(entry);
            return entry;
        }

        void WriteDirs(BinaryWriter oneFile)
        {
            Action<FileType> writeEntry = (FileType type) =>
              (from entry in Files where entry.Type == type select entry).Single().Write(oneFile);

            Func<FileType, int> writeEntries = (FileType type) =>
            {
                IEnumerable<FileEntry> entries = from file in Files where file.Type == type select file;
                foreach (FileEntry entry in entries)
                    entry.Write(oneFile);
                return entries.Count();
            };

            FileEntryStart = oneFile.BaseStream.Position;

            if (flags.HasFlag(BundleFlags.HasDepsJson))
                writeEntry(FileType.DepsJson);

            if (flags.HasFlag(BundleFlags.HasRuntimeConfigJson))
                writeEntry(FileType.RuntimeConfigJson);

            writeEntry(FileType.TheApp);

            // Count is +1 For the main app already written.
            AssemblyCount = writeEntries(FileType.Assembly) + 1;
            writeEntries(FileType.Other);
        }

        public void Write(BinaryWriter oneFile)
        {
            long startOffset = oneFile.BaseStream.Position;
            WriteDirs(oneFile);

            oneFile.Write(MajorVersion);
            oneFile.Write(MinorVersion);

            oneFile.Write(FileEntryStart);
            oneFile.Write(Files.Count());
            oneFile.Write(AssemblyCount);
            oneFile.Write((int)flags);

            oneFile.Write(Signature);

            long size = oneFile.BaseStream.Position - startOffset;
            UI.Log($"Manifest: Offset={startOffset}, Size={size}");
        }

        void ReadDirs(BinaryReader oneFile, long fileCount)
        {
            // This extractor doesn't actually care about the type of file.
            // It just extracts out all files. 
            oneFile.BaseStream.Position = FileEntryStart;
            for (long i = 0; i < fileCount; i++)
                Files.Add(FileEntry.Read(oneFile));
        }

        public void Read(BinaryReader oneFile)
        {
            if(oneFile.BaseStream.Length < FooterLength)
                throw new BundleException("Invalid Bundle");

            oneFile.BaseStream.Position = oneFile.BaseStream.Length - SignatureLength;
            string signature = oneFile.ReadString();

            if (!signature.Equals(Signature))
                throw new BundleException("Invalid Bundle");

            oneFile.BaseStream.Position = oneFile.BaseStream.Length - FooterLength;

            uint majorVersion = oneFile.ReadUInt32();
            uint minorVersion = oneFile.ReadUInt32();

            if (majorVersion != MajorVersion || minorVersion != MinorVersion)
                throw new BundleException("Extraction failed: Invalid Version");

            FileEntryStart = oneFile.ReadInt64();
            int fileCount = oneFile.ReadInt32();
            AssemblyCount = oneFile.ReadInt32();
            flags = (BundleFlags)oneFile.ReadInt32();
            ReadDirs(oneFile, fileCount);
        }
    }

    public class Bundle
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

        public Bundle(string hostName, string contentDir, string outputDir, bool embedPDBs)
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
                return FileType.TheApp;

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

    public class Extract
    {
        string OutputDir;
        string SingleFilePath;

        public Extract(string singleFilePath, string outputDir)
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
    public enum RunMode
    {
        Help,
        Bundle,
        Extract
    };

    public static class UI
    {
        static RunMode Mode = RunMode.Bundle;

        // Common Options:
        static bool Verbose = false;
        static string OutputDir;

        // Bundle options:
        static bool EmbedPDBs = false;
        static string HostName;
        static string ContentDir;

        // Extract options:
        static string BundleToExtract;

        // Typical usages are:
        // Bundle: bundle -d <publish-dir> -a <host-exe>
        // Extract: bundle -e <single-exe>
        static void Usage()
        {
            Console.WriteLine($".NET Core Bundler ({Bundle.Version})");
            Console.WriteLine("bundle [<mode>] [<options>]");
            Console.WriteLine("where <Mode> is one of:");
            Console.WriteLine("  Embed mode (by default)");
            Console.WriteLine("  Extract mode (triggered by -e)");
            Console.WriteLine("Embed mode options:");
            Console.WriteLine("  -d <path>  Directory containing the files to bundle");
            Console.WriteLine("  -a <name>  Application host (within the content directory)");
            Console.WriteLine(" [-pdb+]     Embed the PDB file");
            Console.WriteLine("Extract mode options:");
            Console.WriteLine("  -e <path>  Path to the bundle file to extract");
            Console.WriteLine("Common options:");
            Console.WriteLine(" [-o <path>] Output directory (default: current)");
            Console.WriteLine(" [-v]        Generate verbose output");
            Console.WriteLine(" [-?]        Display usage information");
        }

        static void ParseArgs(string[] args)
        {
            int i = 0;
            Func<string, string> NextArg = (string option) =>
            {
                if (++i >= args.Length)
                    throw new BundleArgumentException("Argument missing for" + option);
                return args[i];
            };

            for (; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLower())
                {
                    case "-?":
                    case "-h":
                        Mode = RunMode.Help;
                        break;

                    case "-e":
                        Mode = RunMode.Extract;
                        BundleToExtract = NextArg(arg);
                        break;

                    case "-v":
                        Verbose = true;
                        break;

                    case "-a":
                        HostName = NextArg(arg);
                        break;

                    case "-d":
                        ContentDir = NextArg(arg);
                        break;

                    case "-o":
                        OutputDir = NextArg(arg);
                        break;

                    case "-pdb+":
                        EmbedPDBs = true;
                        break;
                }
            }

            if (Mode == RunMode.Bundle)
            {
                if (ContentDir == null)
                    throw new BundleArgumentException("Missing argument: -d");

                if (HostName == null)
                    throw new BundleArgumentException("Missing argument: -a");
            }

            if (OutputDir == null)
                OutputDir = Environment.CurrentDirectory;
        }

        public static void Log(string fmt, params object[] args)
        {
            if (Verbose)
            {
                Console.WriteLine("LOG: " + fmt, args);
            }
        }

        static void Fail(string type, string message)
        {
            Console.Error.WriteLine($"{type}: {message}");
        }

        public static int Main(string[] args)
        {
            try
            {
                Log($"Bundler version: {Bundle.Version}");
                ParseArgs(args);

                switch(Mode)
                {
                    case RunMode.Help:
                        Usage();
                        return 0;

                    case RunMode.Bundle:
                        Log($"Bundle from dir: {ContentDir}");
                        Log($"Output Directory: {OutputDir}");
                        Bundle bundle = new Bundle(HostName, ContentDir, OutputDir, EmbedPDBs);
                        bundle.MakeBundle();
                        break;

                    case RunMode.Extract:
                        Log($"Extract from file: {BundleToExtract}");
                        Log($"Output Directory: {OutputDir}");
                        Extract extract = new Extract(BundleToExtract, OutputDir);
                        extract.Spill();
                        break;
                }

            }
            catch (BundleArgumentException e)
            {
                Fail("ERROR", e.Message);
                Usage();
                return -1;
            }
            catch (BundleException e)
            {
                Fail("ERROR", e.Message);
                return -2;
            }
            catch (Exception e)
            {
                Fail("INTERNAL ERROR", e.Message);
                return -3;
            }

            return 0;
        }
    }
}

