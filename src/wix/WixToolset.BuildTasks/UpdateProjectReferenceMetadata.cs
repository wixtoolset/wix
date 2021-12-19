// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task adds publish metadata to the appropriate project references.
    /// </summary>
    public class UpdateProjectReferenceMetadata : Task
    {
        /// <summary>
        /// The list of project references that exist.
        /// </summary>
        [Required]
        public ITaskItem[] ProjectReferences { get; set; }

        [Required]
        public string IntermediateFolder { get; set; }

        [Output]
        public ITaskItem[] UpdatedProjectReferences { get; private set; }

        /// <summary>
        /// Finds all project references requesting publishing and updates them to publish instead of build.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var publishProjectReferences = new List<ITaskItem>();
            var intermediateFolder = Path.GetFullPath(this.IntermediateFolder);

            foreach (var projectReference in this.ProjectReferences)
            {
                var publish = projectReference.GetMetadata("Publish");
                var publishDir = projectReference.GetMetadata("PublishDir");

                if (publish.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    (String.IsNullOrWhiteSpace(publish) && !String.IsNullOrWhiteSpace(publishDir)))
                {
                    publishDir = String.IsNullOrWhiteSpace(publishDir) ? this.CalculatePublishDirFromProjectReference(projectReference, intermediateFolder) : Path.GetFullPath(publishDir);

                    this.AddPublishPropertiesToProjectReference(projectReference, publishDir);

                    publishProjectReferences.Add(projectReference);
                }
            }

            this.UpdatedProjectReferences = publishProjectReferences.ToArray();

            return true;
        }

        private string CalculatePublishDirFromProjectReference(ITaskItem projectReference, string intermediateFolder)
        {
            var publishDir = Path.Combine("publish", Path.GetFileNameWithoutExtension(projectReference.ItemSpec));

            return Path.Combine(intermediateFolder, publishDir);
        }

        private void AddPublishPropertiesToProjectReference(ITaskItem projectReference, string publishDir)
        {
            var additionalProperties = projectReference.GetMetadata("AdditionalProperties");
            if (!String.IsNullOrWhiteSpace(additionalProperties))
            {
                additionalProperties += ";";
            }

            additionalProperties += "PublishDir=" + publishDir;

            var bindPath = ToolsCommon.GetMetadataOrDefault(projectReference, "BindPath", publishDir);

            var publishTargets = projectReference.GetMetadata("PublishTargets");
            if (String.IsNullOrWhiteSpace(publishTargets))
            {
                publishTargets = "Publish;GetTargetPath";
            }
            else if (!publishTargets.EndsWith(";GetTargetsPath", StringComparison.OrdinalIgnoreCase))
            {
                publishTargets += ";GetTargetsPath";
            }

            projectReference.SetMetadata("AdditionalProperties", additionalProperties);
            projectReference.SetMetadata("BindPath", bindPath);
            projectReference.SetMetadata("Targets", publishTargets);

            this.Log.LogMessage(MessageImportance.Low, "Adding publish metadata to project reference {0} Targets {1}, BindPath {2}, AdditionalProperties: {3}",
                projectReference.ItemSpec, projectReference.GetMetadata("Targets"), projectReference.GetMetadata("BindPath"), projectReference.GetMetadata("AdditionalProperties"));
        }
    }
}
