// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task adds mutli-targeting and publish metadata to the appropriate project references.
    /// </summary>
    public class UpdateProjectReferenceMetadata : Task
    {
        private static readonly char[] MetadataPairSplitter = new char[] { '|' };

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
        /// Finds all project references requesting multi-targeting and publishing and updates them to publish instead of build and
        /// sets target framework if requested.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var intermediateFolder = Path.GetFullPath(this.IntermediateFolder);

            // Create the project reference facades.
            var projectReferenceFacades = this.ProjectReferences.Select(p => ProjectReferenceFacade.CreateFacade(p, this.Log, intermediateFolder));

            // Expand the facade count by applying Configurations/Platforms.
            projectReferenceFacades = this.ExpandProjectReferencesForConfigurationsAndPlatforms(projectReferenceFacades);

            // Expand the facade count by applying TargetFrameworks/RuntimeIdentifiers.
            projectReferenceFacades = this.ExpandProjectReferencesForTargetFrameworksAndRuntimeIdentifiers(projectReferenceFacades);

            // Assign any metadata added during expansion above to the project references.
            this.UpdatedProjectReferences = this.AssignMetadataToProjectReferences(projectReferenceFacades).ToArray();

            return true;
        }

        private IEnumerable<ProjectReferenceFacade> ExpandProjectReferencesForConfigurationsAndPlatforms(IEnumerable<ProjectReferenceFacade> projectReferenceFacades)
        {
            foreach (var projectReferenceFacade in projectReferenceFacades)
            {
                var configurationsWithPlatforms = ExpandTerms(projectReferenceFacade.AvailableConfigurations, projectReferenceFacade.AvailablePlatforms).ToList();

                if (configurationsWithPlatforms.Count == 0)
                {
                    yield return projectReferenceFacade;
                }
                else
                {
                    var expand = new List<ProjectReferenceFacade>(configurationsWithPlatforms.Count)
                    {
                        projectReferenceFacade
                    };

                    // First, clone the project reference so there are enough facades for all of the
                    // requested configurations/platforms.
                    for (var i = 1; i < configurationsWithPlatforms.Count; ++i)
                    {
                        expand.Add(projectReferenceFacade.Clone());
                    }

                    // Then set the configuration/platform on each project reference.
                    for (var i = 0; i < configurationsWithPlatforms.Count; ++i)
                    {
                        expand[i].Configuration = configurationsWithPlatforms[i].FirstTerm;
                        expand[i].Platform = configurationsWithPlatforms[i].SecondTerm;

                        yield return expand[i];
                    }
                }
            }
        }

        private IEnumerable<ProjectReferenceFacade> ExpandProjectReferencesForTargetFrameworksAndRuntimeIdentifiers(IEnumerable<ProjectReferenceFacade> projectReferenceFacades)
        {
            foreach (var projectReferenceFacade in projectReferenceFacades)
            {
                var tfmsWithRids = ExpandTerms(projectReferenceFacade.AvailableTargetFrameworks, projectReferenceFacade.AvailableRuntimeIdentifiers).ToList();

                if (tfmsWithRids.Count == 0)
                {
                    yield return projectReferenceFacade;
                }
                else
                {
                    var expand = new List<ProjectReferenceFacade>(tfmsWithRids.Count)
                    {
                        projectReferenceFacade
                    };

                    // First, clone the project reference so there are enough facades for all of the
                    // requested target frameworks/runtime identifiers.
                    for (var i = 1; i < tfmsWithRids.Count; ++i)
                    {
                        expand.Add(projectReferenceFacade.Clone());
                    }

                    // Then set the target framework/runtime identifier on each project reference.
                    for (var i = 0; i < tfmsWithRids.Count; ++i)
                    {
                        expand[i].TargetFramework = tfmsWithRids[i].FirstTerm;
                        expand[i].RuntimeIdentifier = tfmsWithRids[i].SecondTerm;

                        yield return expand[i];
                    }
                }
            }
        }

        private IEnumerable<ITaskItem> AssignMetadataToProjectReferences(IEnumerable<ProjectReferenceFacade> facades)
        {
            foreach (var facade in facades)
            {
                var projectReference = facade.ProjectReference;
                var targetsValue = new MetadataValueList(projectReference, "Targets");

                if (facade.Modified)
                {
                    var configurationValue = new MetadataValue(projectReference, "Configuration");
                    var platformValue = new MetadataValue(projectReference, "Platform");
                    var fullConfigurationValue = new MetadataValue(projectReference, "FullConfiguration");
                    var additionalProperties = new MetadataValueList(projectReference, "AdditionalProperties");
                    var bindName = new MetadataValue(projectReference, "BindName");
                    var bindPath = new MetadataValue(projectReference, "BindPath");

                    var publishDir = facade.CalculatePublishDir();

                    additionalProperties.SetValue("PublishDir=", publishDir);
                    additionalProperties.SetValue("RuntimeIdentifier=", facade.RuntimeIdentifier);

                    additionalProperties.Apply();

                    if (!String.IsNullOrWhiteSpace(facade.Configuration))
                    {
                        projectReference.SetMetadata("SetConfiguration", $"Configuration={facade.Configuration}");

                        if (configurationValue.HadValue)
                        {
                            configurationValue.SetValue(facade.Configuration);
                            configurationValue.Apply();
                        }
                    }

                    if (!String.IsNullOrWhiteSpace(facade.Platform))
                    {
                        projectReference.SetMetadata("SetPlatform", $"Platform={facade.Platform}");

                        if (platformValue.HadValue)
                        {
                            platformValue.SetValue(facade.Platform);
                            platformValue.Apply();
                        }
                    }

                    if (fullConfigurationValue.HadValue && (configurationValue.Modified || platformValue.Modified))
                    {
                        fullConfigurationValue.SetValue($"{configurationValue.Value}|{platformValue.Value}");
                        fullConfigurationValue.Apply();
                    }

                    if (!String.IsNullOrWhiteSpace(facade.TargetFramework))
                    {
                        projectReference.SetMetadata("SetTargetFramework", $"TargetFramework={facade.TargetFramework}");
                    }

                    var bindNameSuffix = facade.CalculateBindNameSuffix();
                    if (!String.IsNullOrWhiteSpace(bindNameSuffix))
                    {
                        var bindNamePrefix = bindName.HadValue ? bindName.Value : Path.GetFileNameWithoutExtension(projectReference.ItemSpec);

                        bindName.SetValue(ToolsCommon.CreateIdentifierFromValue(bindNamePrefix + bindNameSuffix));
                        bindName.Apply();
                    }

                    if (!bindPath.HadValue)
                    {
                        bindPath.SetValue(publishDir);
                        bindPath.Apply();
                    }
                }

                if (facade.Publish)
                {
                    var publishTargets = new MetadataValueList(projectReference, "PublishTargets");

                    if (publishTargets.HadValue)
                    {
                        targetsValue.AddRange(publishTargets.Values);
                    }
                    else
                    {
                        targetsValue.SetValue(null, "Publish");
                    }

                    // GetTargetPath target always needs to be last so we can set bind paths to the output location of the project reference.
                    if (targetsValue.Values.Count == 0 || !"GetTargetPath".Equals(targetsValue.Values[targetsValue.Values.Count - 1], StringComparison.OrdinalIgnoreCase))
                    {
                        targetsValue.Values.Remove("GetTargetPath");
                        targetsValue.SetValue(null, "GetTargetPath");
                    }
                }

                if (targetsValue.Modified)
                {
                    targetsValue.Apply();
                }

                this.Log.LogMessage(MessageImportance.Low, "Adding metadata to project reference {0} Targets {1}, BindPath {2}={3}, AdditionalProperties: {4}",
                    projectReference.ItemSpec, projectReference.GetMetadata("Targets"), projectReference.GetMetadata("BindName"), projectReference.GetMetadata("BindPath"), projectReference.GetMetadata("AdditionalProperties"));

                yield return projectReference;
            }
        }

        private static IEnumerable<ExpansionTerms> ExpandTerms(IReadOnlyCollection<string> firstTerms, IReadOnlyCollection<string> secondTerms)
        {
            if (firstTerms.Count == 0)
            {
                firstTerms = new[] { String.Empty };
            }

            foreach (var firstTerm in firstTerms)
            {
                var pairSplit = firstTerm.Split(MetadataPairSplitter, 2);

                // No pair indicator so expand the first term by the second term.
                if (pairSplit.Length == 1)
                {
                    if (secondTerms.Count == 0)
                    {
                        yield return new ExpansionTerms(firstTerm, null);
                    }
                    else
                    {
                        foreach (var secondTerm in secondTerms)
                        {
                            yield return new ExpansionTerms(firstTerm, secondTerm);
                        }
                    }
                }
                else // there was a pair like "first|second" or "first|" or "|second" in the first term, so return that value as the pair.
                {
                    yield return new ExpansionTerms(pairSplit[0], pairSplit[1]);
                }
            }
        }

        private class ProjectReferenceFacade
        {
            private string configuration;
            private string platform;
            private string targetFramework;
            private string runtimeIdentifier;

            public ProjectReferenceFacade(ITaskItem projectReference, IReadOnlyCollection<string> availableConfigurations, string configuration, IReadOnlyCollection<string> availablePlatforms, string platform, IReadOnlyCollection<string> availableTargetFrameworks, string targetFramework, IReadOnlyCollection<string> availableRuntimeIdentifiers, string runtimeIdentifier, string publishBaseDir)
            {
                this.ProjectReference = projectReference;
                this.AvailableConfigurations = availableConfigurations;
                this.configuration = configuration;
                this.AvailablePlatforms = availablePlatforms;
                this.platform = platform;
                this.AvailableTargetFrameworks = availableTargetFrameworks;
                this.targetFramework = targetFramework;
                this.AvailableRuntimeIdentifiers = availableRuntimeIdentifiers;
                this.runtimeIdentifier = runtimeIdentifier;
                this.PublishBaseDir = publishBaseDir;
                this.Modified = !String.IsNullOrWhiteSpace(configuration) || !String.IsNullOrWhiteSpace(platform) ||
                                !String.IsNullOrWhiteSpace(targetFramework) || !String.IsNullOrWhiteSpace(runtimeIdentifier) ||
                                !String.IsNullOrWhiteSpace(publishBaseDir);
            }

            public ITaskItem ProjectReference { get; }

            public bool Modified { get; private set; }

            public IReadOnlyCollection<string> AvailableConfigurations { get; }

            public IReadOnlyCollection<string> AvailablePlatforms { get; }

            public IReadOnlyCollection<string> AvailableRuntimeIdentifiers { get; }

            public IReadOnlyCollection<string> AvailableTargetFrameworks { get; }

            public bool Publish => !String.IsNullOrEmpty(this.PublishBaseDir);

            public string PublishBaseDir { get; }

            public string Configuration
            {
                get => this.configuration;
                set => this.configuration = this.SetWithModified(value, this.configuration);
            }

            public string Platform
            {
                get => this.platform;
                set => this.platform = this.SetWithModified(value, this.platform);
            }

            public string TargetFramework
            {
                get => this.targetFramework;
                set => this.targetFramework = this.SetWithModified(value, this.targetFramework);
            }

            public string RuntimeIdentifier
            {
                get => this.runtimeIdentifier;
                set => this.runtimeIdentifier = this.SetWithModified(value, this.runtimeIdentifier);
            }

            public ProjectReferenceFacade Clone()
            {
                return new ProjectReferenceFacade(new TaskItem(this.ProjectReference), this.AvailableConfigurations, this.configuration, this.AvailablePlatforms, this.platform, this.AvailableTargetFrameworks, this.targetFramework, this.AvailableRuntimeIdentifiers, this.runtimeIdentifier, this.PublishBaseDir);
            }

            public static ProjectReferenceFacade CreateFacade(ITaskItem projectReference, TaskLoggingHelper logger, string intermediateFolder)
            {
                var configurationsValue = new MetadataValueList(projectReference, "Configurations");
                var setConfigurationValue = new MetadataValue(projectReference, "SetConfiguration", "Configuration=");
                var platformsValue = new MetadataValueList(projectReference, "Platforms");
                var setPlatformValue = new MetadataValue(projectReference, "SetPlatform", "Platform=");
                var targetFrameworksValue = new MetadataValueList(projectReference, "TargetFrameworks");
                var setTargetFrameworkValue = new MetadataValue(projectReference, "SetTargetFramework", "TargetFramework=");
                var runtimeIdentifiersValue = new MetadataValueList(projectReference, "RuntimeIdentifiers");
                var publishValue = new MetadataValue(projectReference, "Publish", null);
                var publishDirValue = new MetadataValue(projectReference, "PublishDir", null);

                var configurations = GetFromListAndValidateSetValue(configurationsValue, setConfigurationValue, logger, projectReference, "Configuration=Release");

                var platforms = GetFromListAndValidateSetValue(platformsValue, setPlatformValue, logger, projectReference, "Platform=x64");

                var targetFrameworks = GetFromListAndValidateSetValue(targetFrameworksValue, setTargetFrameworkValue, logger, projectReference, "TargetFramework=tfm");

                string publishBaseDir = null;

                if (publishValue.Value.Equals("true", StringComparison.OrdinalIgnoreCase) || (!publishValue.HadValue && publishDirValue.HadValue))
                {
                    if (publishDirValue.HadValue)
                    {
                        publishBaseDir = publishDirValue.Value;
                    }
                    else
                    {
                        publishBaseDir = Path.Combine(intermediateFolder, "publish", Path.GetFileNameWithoutExtension(projectReference.ItemSpec));
                    }
                }

                // If the Properties metadata is specified MSBuild will not use TargetFramework inference and require explicit declaration of
                // our expansions (Configurations, Platforms, TargetFrameworks, RuntimeIdentifiers). Rather that try to interoperate, we'll
                // warn the user that we're disabling our expansion behavior.
                var propertiesValue = projectReference.GetMetadata("Properties");

                if (!String.IsNullOrWhiteSpace(propertiesValue) && (configurationsValue.HadValue || platformsValue.HadValue || targetFrameworksValue.HadValue || runtimeIdentifiersValue.HadValue))
                {
                    logger.LogWarning(
                        "ProjectReference '{0}' specifies 'Properties' metadata. " +
                        "That overrides ProjectReference expansion so the 'Configurations', 'Platforms', 'TargetFrameworks', and 'RuntimeIdentifiers' metadata was ignored. " +
                        "Instead, use the 'AdditionalProperties' metadata to pass properties to the referenced project without disabling ProjectReference expansion.",
                        projectReference.ItemSpec);

                    // Return a facade that does not participate in expansion.
                    return new ProjectReferenceFacade(projectReference, Array.Empty<string>(), null, Array.Empty<string>(), null, Array.Empty<string>(), null, Array.Empty<string>(), null, publishBaseDir);
                }
                else
                {
                    return new ProjectReferenceFacade(projectReference, configurations, null, platforms, null, targetFrameworks, null, runtimeIdentifiersValue.Values, null, publishBaseDir);
                }
            }

            public string CalculatePublishDir()
            {
                if (!this.Publish)
                {
                    return null;
                }

                var publishDir = this.PublishBaseDir;

                if (!String.IsNullOrWhiteSpace(this.Configuration))
                {
                    publishDir = Path.Combine(publishDir, this.Configuration);
                }

                if (!String.IsNullOrWhiteSpace(this.Platform))
                {
                    publishDir = Path.Combine(publishDir, this.Platform);
                }

                if (!String.IsNullOrWhiteSpace(this.TargetFramework))
                {
                    publishDir = Path.Combine(publishDir, this.TargetFramework);
                }

                if (!String.IsNullOrWhiteSpace(this.RuntimeIdentifier))
                {
                    publishDir = Path.Combine(publishDir, this.RuntimeIdentifier);
                }


                return Path.GetFullPath(publishDir);
            }

            public string CalculateBindNameSuffix()
            {
                var sb = new StringBuilder();

                if (!String.IsNullOrWhiteSpace(this.Configuration))
                {
                    sb.AppendFormat(".{0}", this.Configuration);
                }

                if (!String.IsNullOrWhiteSpace(this.Platform))
                {
                    sb.AppendFormat(".{0}", this.Platform);
                }

                if (!String.IsNullOrWhiteSpace(this.TargetFramework))
                {
                    sb.AppendFormat(".{0}", this.TargetFramework);
                }

                if (!String.IsNullOrWhiteSpace(this.RuntimeIdentifier))
                {
                    sb.AppendFormat(".{0}", this.RuntimeIdentifier);
                }

                return sb.ToString();
            }

            private string SetWithModified(string newValue, string oldValue)
            {
                if (String.IsNullOrWhiteSpace(newValue) && String.IsNullOrWhiteSpace(oldValue))
                {
                    return String.Empty;
                }
                else if (oldValue != newValue)
                {
                    this.Modified = true;
                    return newValue;
                }

                return oldValue;
            }

            private static List<string> GetFromListAndValidateSetValue(MetadataValueList listValue, MetadataValue setValue, TaskLoggingHelper logger, ITaskItem projectReference, string setExample)
            {
                var targetFrameworks = listValue.Values;

                if (setValue.HadValue)
                {
                    if (listValue.HadValue)
                    {
                        logger.LogMessage("ProjectReference {0} contains metadata for both {1} and {2}. {2} takes precedent so the {1} value '{3}' will be ignored", projectReference.ItemSpec, setValue.Name, listValue.Name, setValue.OriginalValue);
                    }
                    else if (!setValue.ValidValue)
                    {
                        logger.LogError("ProjectReference {0} contains invalid {1} value '{2}'. The {1} value should look something like '{3}'.", projectReference.ItemSpec, setValue.Name, setValue.OriginalValue, setExample);
                    }
                }

                return targetFrameworks;
            }
        }

        private class ExpansionTerms
        {
            public ExpansionTerms(string firstTerm, string secondTerm)
            {
                this.FirstTerm = firstTerm;
                this.SecondTerm = secondTerm;
            }

            public string FirstTerm { get; }

            public string SecondTerm { get; }
        }
    }
}
