// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Microsoft.Build.Framework;

    /// <summary>
    /// This task refreshes the generated file that contains ComponentGroupRefs
    /// to harvested output.
    /// </summary>
    public class RefreshGeneratedFile : RefreshTask
    {
        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var componentGroupRefs = new ArrayList();

            for (var i = 0; i < this.ProjectReferencePaths.Length; i++)
            {
                var item = this.ProjectReferencePaths[i];

                if (!String.IsNullOrEmpty(item.GetMetadata(DoNotHarvest)))
                {
                    continue;
                }

                var projectPath = item.GetMetadata("MSBuildSourceProjectFile");
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                var referenceName = GetIdentifierFromName(GetMetadataOrDefault(item, "Name", projectName));

                var pogs = item.GetMetadata("RefProjectOutputGroups").Split(';');
                foreach (var pog in pogs)
                {
                    if (!String.IsNullOrEmpty(pog))
                    {
                        componentGroupRefs.Add(String.Format(CultureInfo.InvariantCulture, "{0}.{1}", referenceName, pog));
                    }
                }
            }

            var doc = new XmlDocument();

            var head = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
            doc.AppendChild(head);

            var rootElement = doc.CreateElement("Wix");
            rootElement.SetAttribute("xmlns", "http://wixtoolset.org/schemas/v4/wxs");
            doc.AppendChild(rootElement);

            var fragment = doc.CreateElement("Fragment");
            rootElement.AppendChild(fragment);

            var componentGroup = doc.CreateElement("ComponentGroup");
            componentGroup.SetAttribute("Id", "Product.Generated");
            fragment.AppendChild(componentGroup);

            foreach (string componentGroupRef in componentGroupRefs)
            {
                var componentGroupRefElement = doc.CreateElement("ComponentGroupRef");
                componentGroupRefElement.SetAttribute("Id", componentGroupRef);
                componentGroup.AppendChild(componentGroupRefElement);
            }

            foreach (var item in this.GeneratedFiles)
            {
                var fullPath = item.GetMetadata("FullPath");

                componentGroup.SetAttribute("Id", Path.GetFileNameWithoutExtension(fullPath));
                try
                {
                    doc.Save(fullPath);
                }
                catch (Exception e)
                {
                    // e.Message will be something like: "Access to the path 'fullPath' is denied."
                    this.Log.LogMessage(MessageImportance.High, "Unable to save generated file to '{0}'. {1}", fullPath, e.Message);
                }
            }

            return true;
        }
    }
}
