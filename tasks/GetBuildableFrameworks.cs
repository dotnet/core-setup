// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GetBuildableFrameworks : BuildTask
    {
        [Required]
        public string ProjectJsonPath { get; set; }
        [Required]
        public string OSGroup { get; set; }
        [Output]
        public string Frameworks { get; set; }
        public override bool Execute()
        {
            List<string> frameworks = new List<string>();

            using (TextReader projectFileReader = File.OpenText(ProjectJsonPath))
            {
                var projectJsonReader = new JsonTextReader(projectFileReader);
                var serializer = new JsonSerializer();
                var project = serializer.Deserialize<JObject>(projectJsonReader);

                var frameworksSection = project.Value<JObject>("frameworks");
                foreach (var framework in frameworksSection.Properties())
                {
                    if (OSGroup == "Windows_NT"
                        || framework.Name.StartsWith("netstandard")
                        || framework.Name.StartsWith("netcoreapp"))
                    {
                        frameworks.Add(framework.Name);
                    }
                }
            }

            Frameworks = string.Join(";",frameworks);

            return true;
        }
    }
}
