// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task adds publish metadata to the appropriate project references.
    /// </summary>
    public class UpdateProjectReferenceMetadata : Task
    {
        private static readonly char[] TargetFrameworksSplitter = new char[] { ',', ';' };

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
        /// Finds all project references requesting publishing and updates them to publish instead of build and
        /// sets target framework if requested.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var updatedProjectReferences = new List<ITaskItem>();
            var intermediateFolder = Path.GetFullPath(this.IntermediateFolder);

            foreach (var projectReference in this.ProjectReferences)
            {
                var additionalProjectReferences = new List<ITaskItem>();

                var updatedProjectReference = this.TrySetTargetFrameworksOnProjectReference(projectReference, additionalProjectReferences);

                if (this.TryAddPublishPropertiesToProjectReference(projectReference, intermediateFolder))
                {
                    foreach (var additionalProjectReference in additionalProjectReferences)
                    {
                        this.TryAddPublishPropertiesToProjectReference(additionalProjectReference, intermediateFolder);
                    }

                    updatedProjectReference = true;
                }

                if (updatedProjectReference)
                {
                    updatedProjectReferences.Add(projectReference);
                }

                updatedProjectReferences.AddRange(additionalProjectReferences);
            }

            this.UpdatedProjectReferences = updatedProjectReferences.ToArray();

            return true;
        }

        private bool TryAddPublishPropertiesToProjectReference(ITaskItem projectReference, string intermediateFolder)
        {
            var publish = projectReference.GetMetadata("Publish");
            var publishDir = projectReference.GetMetadata("PublishDir");

            if (publish.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                (String.IsNullOrWhiteSpace(publish) && !String.IsNullOrWhiteSpace(publishDir)))
            {
                if (String.IsNullOrWhiteSpace(publishDir))
                {
                    publishDir = CalculatePublishDirFromProjectReference(projectReference, intermediateFolder);
                }

                publishDir = AppendTargetFrameworkFromProjectReference(projectReference, publishDir);

                publishDir = Path.GetFullPath(publishDir);

                this.AddPublishPropertiesToProjectReference(projectReference, publishDir);

                return true;
            }

            return false;
        }

        private bool TrySetTargetFrameworksOnProjectReference(ITaskItem projectReference, List<ITaskItem> additionalProjectReferences)
        {
            var setTargetFramework = projectReference.GetMetadata("SetTargetFramework");
            var targetFrameworks = projectReference.GetMetadata("TargetFrameworks");
            var targetFrameworksToSet = targetFrameworks.Split(TargetFrameworksSplitter).Where(s => !String.IsNullOrWhiteSpace(s)).ToList();

            if (String.IsNullOrWhiteSpace(setTargetFramework) && targetFrameworksToSet.Count > 0)
            {
                // First, clone the project reference so there are enough duplicates for all of the
                // requested target frameworks.
                for (var i = 1; i < targetFrameworksToSet.Count; ++i)
                {
                    additionalProjectReferences.Add(new TaskItem(projectReference));
                }

                // Then set the target framework on each project reference.
                for (var i = 0; i < targetFrameworksToSet.Count; ++i)
                {
                    var reference = (i == 0) ? projectReference : additionalProjectReferences[i - 1];

                    this.SetTargetFrameworkOnProjectReference(reference, targetFrameworksToSet[i]);
                }

                return true;
            }

            if (!String.IsNullOrWhiteSpace(setTargetFramework) && !String.IsNullOrWhiteSpace(targetFrameworks))
            {
                this.Log.LogWarning("ProjectReference {0} contains metadata for both SetTargetFramework and TargetFrameworks. SetTargetFramework takes precedent so the TargetFrameworks value '{1}' is ignored", projectReference.ItemSpec, targetFrameworks);
            }

            return false;
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

        private void SetTargetFrameworkOnProjectReference(ITaskItem projectReference, string targetFramework)
        {
            projectReference.SetMetadata("SetTargetFramework", $"TargetFramework={targetFramework}");

            var bindName = projectReference.GetMetadata("BindName");
            if (String.IsNullOrWhiteSpace(bindName))
            {
                bindName = Path.GetFileNameWithoutExtension(projectReference.ItemSpec);

                projectReference.SetMetadata("BindName", $"{bindName}.{targetFramework}");
            }

            this.Log.LogMessage(MessageImportance.Low, "Adding target framework metadata to project reference {0} SetTargetFramework: {1}, BindName: {2}",
                                projectReference.ItemSpec, projectReference.GetMetadata("SetTargetFramework"), projectReference.GetMetadata("BindName"));
        }

        private static string CalculatePublishDirFromProjectReference(ITaskItem projectReference, string intermediateFolder)
        {
            var publishDir = Path.Combine("publish", Path.GetFileNameWithoutExtension(projectReference.ItemSpec));

            return Path.Combine(intermediateFolder, publishDir);
        }

        private static string AppendTargetFrameworkFromProjectReference(ITaskItem projectReference, string publishDir)
        {
            var setTargetFramework = projectReference.GetMetadata("SetTargetFramework");

            if (setTargetFramework.StartsWith("TargetFramework=") && setTargetFramework.Length > "TargetFramework=".Length)
            {
                var targetFramework = setTargetFramework.Substring("TargetFramework=".Length);

                publishDir = Path.Combine(publishDir, targetFramework);
            }

            return publishDir;
        }
    }
}
