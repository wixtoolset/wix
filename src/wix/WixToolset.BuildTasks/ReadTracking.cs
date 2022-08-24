// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Read the contents of the tracking file produced by the build command.
    /// </summary>
    public class ReadTracking : Task
    {
        private const string TrackedTypeMetadataName = "TrackedType";
        private static readonly char[] TrackedLineTypePathSeparator = new[] { '\t' };

        /// <summary>
        /// The path to the tracking file.
        /// </summary>
        [Required]
        public ITaskItem File { get; set; }

        /// <summary>
        /// All tracked files.
        /// </summary>
        [Output]
        public ITaskItem[] All { get; private set; }

        /// <summary>
        /// The union of tracked built outputs.
        /// </summary>
        [Output]
        public ITaskItem[] BuiltOutputs { get; private set; }

        /// <summary>
        /// The tracked content built outputs.
        /// </summary>
        [Output]
        public ITaskItem[] BuiltContentOutputs { get; private set; }

        /// <summary>
        /// The tracked target built outputs.
        /// </summary>
        [Output]
        public ITaskItem[] BuiltTargetOutputs { get; private set; }

        /// <summary>
        /// The tracked pdb built outputs.
        /// </summary>
        [Output]
        public ITaskItem[] BuiltPdbOutputs { get; private set; }

        /// <summary>
        /// The tracked copied outputs.
        /// </summary>
        [Output]
        public ITaskItem[] CopiedOutputs { get; private set; }

        /// <summary>
        /// The tracked inputs.
        /// </summary>
        [Output]
        public ITaskItem[] Inputs { get; private set; }

        /// <summary>
        /// The union of all tracked outputs.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs { get; private set; }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var all = new List<ITaskItem>();
            var path = this.File.ItemSpec;

            if (System.IO.File.Exists(path))
            {
                var lines = System.IO.File.ReadAllLines(path);

                foreach (var line in lines)
                {
                    var split = line.Split(TrackedLineTypePathSeparator, 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length == 2)
                    {
                        all.Add(new TaskItem(split[1], new Dictionary<string, string>() { [TrackedTypeMetadataName] = split[0] }));
                    }
                    else
                    {
                        this.Log.LogError($"Failed to parse tracked line: {line}");
                    }
                }
            }

            this.All = all.ToArray();
            this.BuiltContentOutputs = all.Where(t => FilterByTrackedType(t, "BuiltContentOutput")).ToArray();
            this.BuiltTargetOutputs = all.Where(t => FilterByTrackedType(t, "BuiltTargetOutput")).ToArray();
            this.BuiltPdbOutputs = all.Where(t => FilterByTrackedType(t, "BuiltPdbOutput")).ToArray();
            this.CopiedOutputs = all.Where(t => FilterByTrackedType(t, "CopiedOutput")).ToArray();
            this.Inputs = all.Where(t => FilterByTrackedType(t, "Input")).ToArray();

            this.BuiltOutputs = this.BuiltContentOutputs.Concat(this.BuiltTargetOutputs).Concat(this.BuiltPdbOutputs).ToArray();
            this.Outputs = this.BuiltOutputs.Concat(this.CopiedOutputs).ToArray();

            return true;
        }

        private static bool FilterByTrackedType(ITaskItem item, string type)
        {
            return item.GetMetadata(TrackedTypeMetadataName).Equals(type, StringComparison.OrdinalIgnoreCase);
        }
    }
}
