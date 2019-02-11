// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class CreateFrameworkListFile : BuildTask
    {
        /// <summary>
        /// Files to extract basic information from and include in the list.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public string TargetFile { get; set; }

        /// <summary>
        /// Extra attributes to place on the root node.
        /// 
        /// %(Identity): Attribute name.
        /// %(Value): Attribute value.
        /// </summary>
        public ITaskItem[] RootAttributes { get; set; }

        public override bool Execute()
        {
            XAttribute[] rootAttributes = RootAttributes
                ?.Select(item => new XAttribute(item.ItemSpec, item.GetMetadata("Value")))
                .ToArray();

            var frameworkManifest = new XElement("FileList", rootAttributes);

            foreach (var f in Files
                .Where(item =>
                    item.GetMetadata("TargetPath")?.StartsWith("data/") == true &&
                    item.ItemSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(item => new
                {
                    Item = item,
                    AssemblyName = FileUtilities.GetAssemblyName(item.ItemSpec),
                    FileVersion = FileUtilities.GetFileVersion(item.ItemSpec)
                })
                .Where(f => f.AssemblyName != null)
                .OrderBy(f => f.Item.ItemSpec, StringComparer.OrdinalIgnoreCase))
            {
                string publicKeyToken = ToLowercaseHexString(f.AssemblyName.GetPublicKeyToken());

                frameworkManifest.Add(new XElement(
                    "File",
                    new XAttribute("AssemblyName", f.AssemblyName.Name),
                    new XAttribute("PublicKeyToken", publicKeyToken),
                    new XAttribute("AssemblyVersion", f.AssemblyName.Version),
                    new XAttribute("FileVersion", f.FileVersion)));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
            File.WriteAllText(TargetFile, frameworkManifest.ToString());

            return !Log.HasLoggedErrors;
        }

        private static string ToLowercaseHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            return BitConverter.ToString(bytes)
                .ToLowerInvariant()
                .Replace("-", "");
        }
    }
}
