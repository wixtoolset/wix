// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class CreateItemAvoidingInference : Task
    {
        /// <summary>
        /// The properties to converty to items.
        /// </summary>
        [Required]
        public string InputProperties { get; set; }

        /// <summary>
        /// The output items.
        /// </summary>
        [Output]
        public ITaskItem[] OuputItems { get; private set; }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var newItems = new List<ITaskItem>();

            foreach (var property in this.InputProperties.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                newItems.Add(new TaskItem(property));
            }

            this.OuputItems = newItems.ToArray();

            return true;
        }
    }
}
