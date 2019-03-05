using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    public class BundleManifest
    {
        [Flags]
        public enum BundleFlags
        {
            None = 0,
            HasDepsJson = 1,
            HasRuntimeConfigJson = 2
        }

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

            writeEntry(FileType.Application);

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
}

