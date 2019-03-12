// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class RegenerateReadmeTable : BuildTask
    {
        private const string TableComment = "generated table";
        private const string LinksComment = "links to include in table";

        [Required]
        public string ReadmeFile { get; set; }

        [Required]
        public ITaskItem[] Branches { get; set; }

        [Required]
        public ITaskItem[] Platforms { get; set; }

        public override bool Execute()
        {
            string[] readmeLines = File.ReadAllLines(ReadmeFile);

            var links = readmeLines
                .SkipWhile(line => line != Begin(LinksComment))
                .Skip(1)
                .TakeWhile(line => line != End(LinksComment))
                .Where(line => line.StartsWith("[") && line.Contains("]:"))
                .Select(line => line.Substring(1, line.IndexOf("]:", StringComparison.Ordinal) - 1))
                .ToArray();

            var rows = Platforms.Select(p => CreateRow(p, links)).ToArray();

            var table = new[]
            {
                "",
                $"| Platform |{string.Concat(Branches.Select(p => $" {p.ItemSpec} |"))}",
                $"| --- | {string.Concat(Enumerable.Repeat(" :---: |", Branches.Length))}"
            }.Concat(rows).Concat(new[] { "" });

            if (readmeLines.Contains(Begin(TableComment)) &&
                readmeLines.Contains(End(TableComment)))
            {
                string[] beforeTable = readmeLines
                    .TakeWhile(line => line != Begin(TableComment))
                    .Concat(new[] { Begin(TableComment) })
                    .ToArray();

                string[] afterTable = readmeLines
                    .Skip(beforeTable.Length)
                    .SkipWhile(line => line != End(TableComment))
                    .ToArray();

                File.WriteAllLines(
                    ReadmeFile,
                    beforeTable.Concat(table).Concat(afterTable));
            }
            else
            {
                Log.LogError($"Readme '{ReadmeFile}' has no 'BEGIN/END generated table' section.");
            }

            return !Log.HasLoggedErrors;
        }

        private string CreateRow(ITaskItem platform, string[] links)
        {
            string parenthetical = platform.GetMetadata("Parenthetical");

            string cells = string.Concat(
                Branches.Select(branch => $" {CreateCell(platform, branch, links)} |"));

            return $"| **{platform.ItemSpec}**{parenthetical} |{cells}";
        }

        private string CreateCell(ITaskItem platform, ITaskItem branch, string[] links)
        {
            string branchAbbr = branch.GetMetadata("Abbr");
            if (string.IsNullOrEmpty(branchAbbr))
            {
                Log.LogError($"Branch '{branch.ItemSpec}' has no Abbr metadata.");
            }

            string platformAbbr = platform.GetMetadata("Abbr");
            if (string.IsNullOrEmpty(platformAbbr))
            {
                Log.LogError($"Platform '{platform.ItemSpec}' has no Abbr metadata.");
            }

            var sb = new StringBuilder();
            
            string Link(string type) => $"{platformAbbr}-{type}-{branchAbbr}";

            void AddLink(string name, string type)
            {
                string link = Link(type);
                string checksum = Link($"{type}-checksum");

                if (links.Contains(link))
                {
                    sb.Append("<br>");
                    sb.Append($"[{name}][{link}]");
                    if (links.Contains(checksum))
                    {
                        sb.Append($" ([Checksum][{checksum}])");
                    }
                }
            }

            string badge = Link("badge");
            string version = Link("version");

            if (links.Contains(badge) && links.Contains(version))
            {
                sb.Append($"[![][{badge}]][{version}]");
            }

            AddLink("Installer", "installer");

            AddLink("Runtime-Deps", "runtime-deps");
            AddLink("Host", "host");
            AddLink("Host FX Resolver", "hostfxr");
            AddLink("Shared Framework", "sharedfx");

            AddLink("zip", "zip");
            AddLink("tar.gz", "targz");

            AddLink("Symbols (zip)", "symbols-zip");
            AddLink("Symbols (tar.gz)", "symbols-targz");

            if (sb.Length == 0)
            {
                sb.Append("N/A");
            }

            return sb.ToString();
        }

        private string Begin(string marker) => $"<!-- BEGIN {marker} -->";
        private string End(string marker) => $"<!-- END {marker} -->";
    }
}
