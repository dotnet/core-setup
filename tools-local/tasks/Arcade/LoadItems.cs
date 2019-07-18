// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Linq;

namespace Microsoft.DotNet.Arcade.Sdk
{
    /// <summary>
    /// This task reads msbuild Items with their metadata from a props file. Useful when SaveItems
    /// has been used to persist some items to disk, but it's inconvenient to load them statically.
    /// </summary>
    public class LoadItems : Task
    {
        [Required]
        public string File { get; set; }

        /// <summary>
        /// Item name (type) to read. If not set, all items are read.
        /// </summary>
        public string ItemName { get; set; }

        [Output]
        public ITaskItem[] Items { get; set; }

        public override bool Execute()
        {
            var project = ProjectRootElement.Open(File);

            Items = project.Items
                .Where(i =>
                    string.IsNullOrEmpty(ItemName) ||
                    i.ItemType.Equals(ItemName, StringComparison.OrdinalIgnoreCase))
                .Select(i => new TaskItem(
                    i.Include,
                    i.Metadata.ToDictionary(m => m.Name, m => m.Value)))
                .ToArray();
            
            return !Log.HasLoggedErrors;
        }
    }
}
