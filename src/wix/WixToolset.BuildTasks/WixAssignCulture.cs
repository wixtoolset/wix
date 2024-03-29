// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class WixAssignCulture : Task
    {
        private const string CultureAttributeName = "Culture";
        private const string OutputSuffixMetadataName = "OutputSuffix";
        private const string OutputFolderMetadataName = "OutputFolder";
        private const string InvariantCultureIdentifier = "neutral";
        private const string NullCultureIdentifier = "null";

        /// <summary>
        /// The list of cultures to build.  Cultures are specified in the following form:
        ///     primary culture,first fallback culture, second fallback culture;...
        /// Culture groups are seperated by semi-colons 
        /// Culture precedence within a culture group is evaluated from left to right where fallback cultures are 
        /// separated with commas.
        /// The first (primary) culture in a culture group will be used as the output sub-folder.
        /// </summary>
        public string Cultures { get; set; }

        /// <summary>
        /// The list of files to apply culture information to.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The files that had culture information applied
        /// </summary>
        [Output]
        public ITaskItem[] CultureGroups { get; private set; }

        /// <summary>
        /// Applies culture information to the files specified by the Files property.
        /// This task intentionally does not validate that strings are valid Cultures so that we can support
        /// psuedo-loc.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            // First, process the culture group list the user specified in the cultures property
            var cultureGroups = new List<CultureGroup>();

            if (!String.IsNullOrEmpty(this.Cultures))
            {
                // Get rid of extra quotes
                this.Cultures = this.Cultures.Trim('\"');

                // MSBuild cannnot handle "" items for the invariant culture we require the neutral keyword
                foreach (var cultureGroupString in this.Cultures.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var cultureGroup = new CultureGroup(cultureGroupString);
                    cultureGroups.Add(cultureGroup);
                }
            }
            else
            {
                // Only process the EmbeddedResource items if cultures was unspecified
                foreach (var file in this.Files)
                {
                    // Ignore non-wxls
                    if (!String.Equals(file.GetMetadata("Extension"), ".wxl", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}. The file type is not supported.", file.ItemSpec);
                        return false;
                    }

                    var wxlFile = new XmlDocument();
                    try
                    {
                        wxlFile.Load(file.ItemSpec);
                    }
                    catch (FileNotFoundException)
                    {
                        this.Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}. The file was not found.", file.ItemSpec);
                        return false;
                    }
                    catch (Exception e)
                    {
                        this.Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}: {1}", file.ItemSpec, e.Message);
                        return false;
                    }

                    // Take the culture value and try using it to create a culture.
                    var cultureAttr = wxlFile.DocumentElement.Attributes[WixAssignCulture.CultureAttributeName];
                    var wxlCulture = cultureAttr?.Value ?? String.Empty;

                    if (0 == wxlCulture.Length)
                    {
                        // We use a keyword for the invariant culture because MSBuild cannnot handle "" items.
                        wxlCulture = InvariantCultureIdentifier;
                    }

                    // We found the culture for the WXL, we now need to determine if it maps to a culture group specified
                    // in the Cultures property or if we need to create a new one.
                    this.Log.LogMessage(MessageImportance.Low, "Culture \"{0}\" from EmbeddedResource {1}.", wxlCulture, file.ItemSpec);

                    var cultureGroupExists = false;
                    foreach (var cultureGroup in cultureGroups)
                    {
                        foreach (var culture in cultureGroup.Cultures)
                        {
                            if (String.Equals(wxlCulture, culture, StringComparison.OrdinalIgnoreCase))
                            {
                                cultureGroupExists = true;
                                break;
                            }
                        }
                    }

                    // The WXL didn't match a culture group we already have so create a new one.
                    if (!cultureGroupExists)
                    {
                        cultureGroups.Add(new CultureGroup(wxlCulture));
                    }
                }
            }

            // If we didn't create any culture groups the culture was unspecificed and no WXLs were included
            // then build an unlocalized target in the output folder
            if (cultureGroups.Count == 0)
            {
                cultureGroups.Add(new CultureGroup());
            }

            var cultureGroupItems = new List<TaskItem>();

            if (1 == cultureGroups.Count && 0 == this.Files.Length)
            {
                // Maintain old behavior, if only one culturegroup is specified and no WXL, output to the default folder
                var cultureGroupItem = new TaskItem(cultureGroups[0].ToString());
                cultureGroupItem.SetMetadata(OutputSuffixMetadataName, cultureGroups[0].OutputSuffix);
                cultureGroupItem.SetMetadata(OutputFolderMetadataName, CultureGroup.DefaultFolder);
                cultureGroupItems.Add(cultureGroupItem);
            }
            else
            {
                foreach (var cultureGroup in cultureGroups)
                {
                    var cultureGroupItem = new TaskItem(cultureGroup.ToString());
                    cultureGroupItem.SetMetadata(OutputSuffixMetadataName, cultureGroup.OutputSuffix);
                    cultureGroupItem.SetMetadata(OutputFolderMetadataName, cultureGroup.OutputFolder);
                    cultureGroupItems.Add(cultureGroupItem);

                    this.Log.LogMessage("Culture: {0}", cultureGroup.ToString());
                }
            }

            this.CultureGroups = cultureGroupItems.ToArray();
            return true;
        }

        private class CultureGroup
        {
            /// <summary>
            /// TargetPath already has a '\', do not double it!
            /// </summary>
            public const string DefaultFolder = "";

            /// <summary>
            /// Language neutral.
            /// </summary>
            public const string DefaultSuffix = InvariantCultureIdentifier;

            /// <summary>
            /// Initialize a null culture group
            /// </summary>
            public CultureGroup()
            {
            }

            public CultureGroup(string cultureGroupString)
            {
                Debug.Assert(!String.IsNullOrEmpty(cultureGroupString));
                foreach (var cultureString in cultureGroupString.Split(','))
                {
                    this.Cultures.Add(cultureString);
                }
            }

            public List<string> Cultures { get; } = new List<string>();

            public string OutputFolder
            {
                get
                {
                    if (this.Cultures.Count > 0 && 
                        !this.Cultures[0].Equals(InvariantCultureIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        return this.Cultures[0] + "\\";
                    }

                    return DefaultFolder;
                }
            }

            public string OutputSuffix
            {
                get =>  (this.Cultures.Count > 0) ? this.Cultures[0] : InvariantCultureIdentifier;
            }

            public override string ToString()
            {
                if (this.Cultures.Count > 0)
                {
                    return String.Join(";", this.Cultures);
                }

                // We use a keyword for a null culture because MSBuild cannnot handle "" items
                // Null is different from neutral.  For neutral we still want to do WXL
                // filtering in Light.
                return NullCultureIdentifier;
            }
        }
    }
}
