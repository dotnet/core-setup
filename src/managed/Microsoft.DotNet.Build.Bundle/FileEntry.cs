using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    // FileEntry: Records information about embedded files.
    // A manifest of embedded files is recorded in the bundle meta-data.
    // For each embedded file, the following information is written:
    // * File Name Length (Int64)
    // * File Name (<Length> Bytes)
    // * File Offset (Int64)
    // * File Size (Int64)

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
}

