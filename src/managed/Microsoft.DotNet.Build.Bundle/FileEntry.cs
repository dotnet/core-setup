// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    /// FileEntry: Records information about embedded files.
    /// The bundle manifest records the following meta-data for each 
    /// file embedded in the bundle:
    /// * File Name Length (Int64)
    /// * File Name (<Length> Bytes)
    /// * File Offset (Int64)
    /// * File Size (Int64)
    /// 
    /// The Bundle manifest does not encode FileType explicitly in each entry. 
    /// This information is implicitly deduced based on the position of the 
    /// FileEntry in the Manifest, and some additional information in the Manifest footer.
    /// </summary>

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

        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Offset);
            writer.Write(Size);
        }

        public static FileEntry Read(BinaryReader reader, FileType fileType = FileType.Other)
        {
            string fileName = reader.ReadString();
            long offset = reader.ReadInt64();
            long size = reader.ReadInt64();
            return new FileEntry(fileType, fileName, offset, size);
        }

        public override string ToString()
        {
            return String.Format($"{Name} [{Type}] @{Offset} Sz={Size}");
        }
    }
}

