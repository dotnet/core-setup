// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class CopyNupkgAndChangeVersion : BuildTask
    {
        [Required]
        public string SourceFile { get; set; }

        [Required]
        public string TargetFile { get; set; }

        [Required]
        public string OriginalVersion { get; set; }

        [Required]
        public string TargetVersion { get; set; }

        public string[] DependencyPackageIdsToChange { get; set; }

        public override bool Execute()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
            File.Copy(SourceFile, TargetFile, true);

            using (ZipArchive zip = ZipFile.Open(TargetFile, ZipArchiveMode.Update))
            {
                foreach (var nuspec in zip.Entries.Where(e => e.FullName.EndsWith(".nuspec")))
                {
                    Rewrite(nuspec, s =>
                    {
                        XDocument content = XDocument.Parse(s);

                        XNamespace rootNamespace = content.Root.GetDefaultNamespace();
                        XName GetQualifiedName(string name) => rootNamespace.GetName(name);

                        var versionElement = content
                            .Element(GetQualifiedName("package"))
                            .Element(GetQualifiedName("metadata"))
                            .Element(GetQualifiedName("version"));

                        if (versionElement.Value != OriginalVersion)
                        {
                            Log.LogError(
                                $"Original version is '{versionElement.Value}', " +
                                $"expected '{OriginalVersion}'");
                        }

                        versionElement.Value = TargetVersion;

                        foreach (var dependency in content
                            .Descendants(GetQualifiedName("dependency"))
                            .Where(x =>
                                x.Attribute("version").Value == OriginalVersion &&
                                DependencyPackageIdsToChange?.Contains(x.Attribute("id").Value) == true))
                        {
                            dependency.Value = TargetVersion;
                        }

                        return content.ToString();
                    });
                }

                foreach (var runtimeJson in zip.Entries.Where(e => e.FullName == "runtime.json"))
                {
                    Rewrite(runtimeJson, s =>
                    {
                        JObject content = JObject.Parse(s);
                        var versionProperties = content
                            .Descendants()
                            .OfType<JProperty>()
                            .Where(p =>
                                p.Value is JValue v &&
                                v.Type == JTokenType.String);

                        foreach (var p in versionProperties)
                        {
                            var range = VersionRange.Parse(p.Value.Value<string>());

                            if (range.MinVersion.OriginalVersion == OriginalVersion)
                            {
                                var newRange = new VersionRange(
                                    NuGetVersion.Parse(TargetVersion),
                                    range.Float);

                                p.Value = newRange.ToString();
                            }
                        }

                        return content.ToString();
                    });
                }
            }

            return !Log.HasLoggedErrors;
        }

        private void Rewrite(ZipArchiveEntry entry, Func<string, string> rewrite)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                var content = rewrite(reader.ReadToEnd());

                stream.Position = 0;
                stream.SetLength(0);
                writer.Write(content);
            }
        }
    }
}
