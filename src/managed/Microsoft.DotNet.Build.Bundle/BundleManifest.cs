// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    ///  BundleManifest is a description of the contents of a bundle file.
    ///  This class handles creation and consumption of bundle-manifests.
    ///  
    ///  Here is the description of the Bundle Layout:
    ///  _______________________________________________
    ///  AppHost 
    ///
    ///
    /// ----------------------------------------------
    /// The embedded files including the app, its
    /// configuration files, dependencies, and 
    /// possibly the runtime.
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    ///
    /// -----------------------------------------------
    /// Meta-data Directory (FileEntries)
    ///   "app.deps.json"               (if any)
    ///   "app.runtimeconfig.json"      (if any)
    ///   "app.runtimeconfig.dev.json"  (if any)
    ///   "app.dll"                 (The main app)
    ///   "IL_1.dll"                (Assemblies, if any)
    ///    ...
    ///   "IL_n.dll"
    ///   "File_1"                  (Other Files, if any)
    ///    ...
    ///   "File_n" 
    /// - - - - - - - - - - - - - - - - - - - - - - - -
    ///  Bundle Manifest (footer)
    ///     MajorVersion
    ///     MinorVersion
    ///     FileEntryStart (offset)
    ///     NumFiles
    ///     NumAssemblies
    ///     Flags
    /// -----------------------------------------------
    ///   Bundle Signature
    /// _________________________________________________
    /// 
    /// The Bundle manifest does not encode FileType explicitly in each entry. 
    /// The FileType for each embedded file is deduced based on:
    ///  - FileEntry position: FileEntries are recorded in the order
    ///     -- Configuration files
    ///     -- The main assembly
    ///     -- Other assemblies
    ///     -- Other Files
    ///  - Flags (to determine number of configuration files)
    ///  - NumAssemblies 
    /// </summary>

    public class BundleManifest
    {
        [Flags]
        enum BundleFlags
        {
            None = 0,
            HasDepsJson = 1,
            HasRuntimeConfigJson = 2,
            HasRuntimeConfigDevJson = 4
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
                case FileType.RuntimeConfigDevJson:
                    flags |= BundleFlags.HasRuntimeConfigDevJson;
                    break;

                default:
                    break;
            }

            FileEntry entry = new FileEntry(type, name, offset, size);
            Files.Add(entry);
            return entry;
        }

        void WriteFileEntries(BinaryWriter writer)
        {
            Action<FileType> writeEntry = (FileType type) =>
                Files
                .Where(entry => entry.Type == type)
                .Single()
                .Write(writer);

            Func<FileType, int> writeEntries = (FileType type) =>
            {
                IEnumerable<FileEntry> entries = Files.Where(entry => entry.Type == type);
                foreach (FileEntry entry in entries)
                    entry.Write(writer);
                return entries.Count();
            };

            FileEntryStart = writer.BaseStream.Position;

            if (flags.HasFlag(BundleFlags.HasDepsJson))
                writeEntry(FileType.DepsJson);

            if (flags.HasFlag(BundleFlags.HasRuntimeConfigJson))
                writeEntry(FileType.RuntimeConfigJson);

            if (flags.HasFlag(BundleFlags.HasRuntimeConfigDevJson))
                writeEntry(FileType.RuntimeConfigDevJson);

            writeEntry(FileType.Application);

            // Count is +1 For the main app already written.
            AssemblyCount = writeEntries(FileType.Assembly) + 1;
            writeEntries(FileType.Other);
        }

        public void Write(BinaryWriter writer)
        {
            long startOffset = writer.BaseStream.Position;
            WriteFileEntries(writer);

            writer.Write(MajorVersion);
            writer.Write(MinorVersion);

            writer.Write(FileEntryStart);
            writer.Write(Files.Count());
            writer.Write(AssemblyCount);
            writer.Write((int)flags);

            writer.Write(Signature);

            long size = writer.BaseStream.Position - startOffset;
            Program.Log($"Manifest: Offset={startOffset}, Size={size}");
        }

        void ReadFileEntries(BinaryReader reader, long fileCount)
        {
            // This extractor doesn't actually care about the type of file.
            // It just extracts out all files. 
            reader.BaseStream.Position = FileEntryStart;
            for (long i = 0; i < fileCount; i++)
                Files.Add(FileEntry.Read(reader));
        }

        public void Read(BinaryReader reader)
        {
            if(reader.BaseStream.Length < FooterLength)
                throw new BundleException("Invalid Bundle");

            reader.BaseStream.Position = reader.BaseStream.Length - SignatureLength;
            string signature = reader.ReadString();

            if (!signature.Equals(Signature))
                throw new BundleException("Invalid Bundle");

            reader.BaseStream.Position = reader.BaseStream.Length - FooterLength;

            uint majorVersion = reader.ReadUInt32();
            uint minorVersion = reader.ReadUInt32();

            if (majorVersion != MajorVersion || minorVersion != MinorVersion)
                throw new BundleException("Extraction failed: Invalid Version");

            FileEntryStart = reader.ReadInt64();
            int fileCount = reader.ReadInt32();
            AssemblyCount = reader.ReadInt32();
            flags = (BundleFlags)reader.ReadInt32();
            ReadFileEntries(reader, fileCount);
        }
    }
}

