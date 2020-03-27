// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    internal class LegacySearchFacade : BaseSearchFacade
    {
        public LegacySearchFacade(WixSearchTuple searchTuple, IntermediateTuple searchSpecificTuple)
        {
            this.SearchTuple = searchTuple;
            this.SearchSpecificTuple = searchSpecificTuple;
        }

        public IntermediateTuple SearchSpecificTuple { get; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup a search.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlTextWriter writer)
        {
            switch (this.SearchSpecificTuple)
            {
                case WixComponentSearchTuple tuple:
                    this.WriteComponentSearchXml(writer, tuple);
                    break;
                case WixFileSearchTuple tuple:
                    this.WriteFileSearchXml(writer, tuple);
                    break;
                case WixProductSearchTuple tuple:
                    this.WriteProductSearchXml(writer, tuple);
                    break;
                case WixRegistrySearchTuple tuple:
                    this.WriteRegistrySearchXml(writer, tuple);
                    break;
            }
        }

        private void WriteComponentSearchXml(XmlTextWriter writer, WixComponentSearchTuple searchTuple)
        {
            writer.WriteStartElement("MsiComponentSearch");

            base.WriteXml(writer);

            writer.WriteAttributeString("ComponentId", searchTuple.Guid);

            if (!String.IsNullOrEmpty(searchTuple.ProductCode))
            {
                writer.WriteAttributeString("ProductCode", searchTuple.ProductCode);
            }

            if (0 != (searchTuple.Attributes & WixComponentSearchAttributes.KeyPath))
            {
                writer.WriteAttributeString("Type", "keyPath");
            }
            else if (0 != (searchTuple.Attributes & WixComponentSearchAttributes.State))
            {
                writer.WriteAttributeString("Type", "state");
            }
            else if (0 != (searchTuple.Attributes & WixComponentSearchAttributes.WantDirectory))
            {
                writer.WriteAttributeString("Type", "directory");
            }

            writer.WriteEndElement();
        }

        private void WriteFileSearchXml(XmlTextWriter writer, WixFileSearchTuple searchTuple)
        {
            writer.WriteStartElement((0 == (searchTuple.Attributes & WixFileSearchAttributes.IsDirectory)) ? "FileSearch" : "DirectorySearch");

            base.WriteXml(writer);

            writer.WriteAttributeString("Path", searchTuple.Path);
            if (WixFileSearchAttributes.WantExists == (searchTuple.Attributes & WixFileSearchAttributes.WantExists))
            {
                writer.WriteAttributeString("Type", "exists");
            }
            else if (WixFileSearchAttributes.WantVersion == (searchTuple.Attributes & WixFileSearchAttributes.WantVersion))
            {
                // Can never get here for DirectorySearch.
                writer.WriteAttributeString("Type", "version");
            }
            else
            {
                writer.WriteAttributeString("Type", "path");
            }
            writer.WriteEndElement();
        }

        private void WriteProductSearchXml(XmlTextWriter writer, WixProductSearchTuple tuple)
        {
            writer.WriteStartElement("MsiProductSearch");

            base.WriteXml(writer);

            if (0 != (tuple.Attributes & WixProductSearchAttributes.UpgradeCode))
            {
                writer.WriteAttributeString("UpgradeCode", tuple.Guid);
            }
            else
            {
                writer.WriteAttributeString("ProductCode", tuple.Guid);
            }

            if (0 != (tuple.Attributes & WixProductSearchAttributes.Version))
            {
                writer.WriteAttributeString("Type", "version");
            }
            else if (0 != (tuple.Attributes & WixProductSearchAttributes.Language))
            {
                writer.WriteAttributeString("Type", "language");
            }
            else if (0 != (tuple.Attributes & WixProductSearchAttributes.State))
            {
                writer.WriteAttributeString("Type", "state");
            }
            else if (0 != (tuple.Attributes & WixProductSearchAttributes.Assignment))
            {
                writer.WriteAttributeString("Type", "assignment");
            }

            writer.WriteEndElement();
        }

        private void WriteRegistrySearchXml(XmlTextWriter writer, WixRegistrySearchTuple tuple)
        {
            writer.WriteStartElement("RegistrySearch");

            base.WriteXml(writer);

            switch (tuple.Root)
            {
                case RegistryRootType.ClassesRoot:
                    writer.WriteAttributeString("Root", "HKCR");
                    break;
                case RegistryRootType.CurrentUser:
                    writer.WriteAttributeString("Root", "HKCU");
                    break;
                case RegistryRootType.LocalMachine:
                    writer.WriteAttributeString("Root", "HKLM");
                    break;
                case RegistryRootType.Users:
                    writer.WriteAttributeString("Root", "HKU");
                    break;
            }

            writer.WriteAttributeString("Key", tuple.Key);

            if (!String.IsNullOrEmpty(tuple.Value))
            {
                writer.WriteAttributeString("Value", tuple.Value);
            }

            var existenceOnly = 0 != (tuple.Attributes & WixRegistrySearchAttributes.WantExists);

            writer.WriteAttributeString("Type", existenceOnly ? "exists" : "value");

            if (0 != (tuple.Attributes & WixRegistrySearchAttributes.Win64))
            {
                writer.WriteAttributeString("Win64", "yes");
            }

            if (!existenceOnly)
            {
                if (0 != (tuple.Attributes & WixRegistrySearchAttributes.ExpandEnvironmentVariables))
                {
                    writer.WriteAttributeString("ExpandEnvironment", "yes");
                }

                // We *always* say this is VariableType="string". If we end up
                // needing to be more specific, we will have to expand the "Format"
                // attribute to allow "number" and "version".

                writer.WriteAttributeString("VariableType", "string");
            }

            writer.WriteEndElement();
        }
    }
}
