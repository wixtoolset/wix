// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task creates items from properties without triggering the item creation inference
    /// MSBuild targets will normally do. There are very specialized cases where this is used.
    /// </summary>
    public class CreateItemAvoidingInference : Task
    {
        /// <summary>
        /// The properties to convert into items.
        /// </summary>
        [Required]
        public string InputProperties { get; set; }

        /// <summary>
        /// The output items.
        /// </summary>
        [Output]
        public ITaskItem[] OutputItems { get; private set; }

        /// <summary>
        /// Convert the input properties into output items.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            this.OutputItems = this.InputProperties
                                   .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(property => new TaskItem(property))
                                   .ToArray();

            return true;
        }
    }
}
