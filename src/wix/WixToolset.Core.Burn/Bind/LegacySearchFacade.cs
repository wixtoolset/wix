// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class LegacySearchFacade : BaseSearchFacade
    {
        public LegacySearchFacade(WixSearchSymbol searchSymbol, IntermediateSymbol searchSpecificSymbol)
        {
            this.SearchSymbol = searchSymbol;
            this.SearchSpecificSymbol = searchSpecificSymbol;
        }

        public IntermediateSymbol SearchSpecificSymbol { get; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup a search.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlTextWriter writer)
        {
            switch (this.SearchSpecificSymbol)
            {
                case WixComponentSearchSymbol symbol:
                    this.WriteComponentSearchXml(writer, symbol);
                    break;
                case WixFileSearchSymbol symbol:
                    this.WriteFileSearchXml(writer, symbol);
                    break;
                case WixProductSearchSymbol symbol:
                    this.WriteProductSearchXml(writer, symbol);
                    break;
                case WixRegistrySearchSymbol symbol:
                    this.WriteRegistrySearchXml(writer, symbol);
                    break;
            }
        }

        private void WriteComponentSearchXml(XmlTextWriter writer, WixComponentSearchSymbol searchSymbol)
        {
            writer.WriteStartElement("MsiComponentSearch");

            base.WriteXml(writer);

            writer.WriteAttributeString("ComponentId", searchSymbol.Guid);

            if (!String.IsNullOrEmpty(searchSymbol.ProductCode))
            {
                writer.WriteAttributeString("ProductCode", searchSymbol.ProductCode);
            }

            if (0 != (searchSymbol.Attributes & WixComponentSearchAttributes.KeyPath))
            {
                writer.WriteAttributeString("Type", "keyPath");
            }
            else if (0 != (searchSymbol.Attributes & WixComponentSearchAttributes.State))
            {
                writer.WriteAttributeString("Type", "state");
            }
            else if (0 != (searchSymbol.Attributes & WixComponentSearchAttributes.WantDirectory))
            {
                writer.WriteAttributeString("Type", "directory");
            }

            writer.WriteEndElement();
        }

        private void WriteFileSearchXml(XmlTextWriter writer, WixFileSearchSymbol searchSymbol)
        {
            writer.WriteStartElement((0 == (searchSymbol.Attributes & WixFileSearchAttributes.IsDirectory)) ? "FileSearch" : "DirectorySearch");

            base.WriteXml(writer);

            writer.WriteAttributeString("Path", searchSymbol.Path);
            if (WixFileSearchAttributes.WantExists == (searchSymbol.Attributes & WixFileSearchAttributes.WantExists))
            {
                writer.WriteAttributeString("Type", "exists");
            }
            else if (WixFileSearchAttributes.WantVersion == (searchSymbol.Attributes & WixFileSearchAttributes.WantVersion))
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

        private void WriteProductSearchXml(XmlTextWriter writer, WixProductSearchSymbol symbol)
        {
            writer.WriteStartElement("MsiProductSearch");

            base.WriteXml(writer);

            if (0 != (symbol.Attributes & WixProductSearchAttributes.UpgradeCode))
            {
                writer.WriteAttributeString("UpgradeCode", symbol.Guid);
            }
            else
            {
                writer.WriteAttributeString("ProductCode", symbol.Guid);
            }

            if (0 != (symbol.Attributes & WixProductSearchAttributes.Version))
            {
                writer.WriteAttributeString("Type", "version");
            }
            else if (0 != (symbol.Attributes & WixProductSearchAttributes.Language))
            {
                writer.WriteAttributeString("Type", "language");
            }
            else if (0 != (symbol.Attributes & WixProductSearchAttributes.State))
            {
                writer.WriteAttributeString("Type", "state");
            }
            else if (0 != (symbol.Attributes & WixProductSearchAttributes.Assignment))
            {
                writer.WriteAttributeString("Type", "assignment");
            }

            writer.WriteEndElement();
        }

        private void WriteRegistrySearchXml(XmlTextWriter writer, WixRegistrySearchSymbol symbol)
        {
            writer.WriteStartElement("RegistrySearch");

            base.WriteXml(writer);

            switch (symbol.Root)
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

            writer.WriteAttributeString("Key", symbol.Key);

            if (!String.IsNullOrEmpty(symbol.Value))
            {
                writer.WriteAttributeString("Value", symbol.Value);
            }

            var existenceOnly = 0 != (symbol.Attributes & WixRegistrySearchAttributes.WantExists);

            writer.WriteAttributeString("Type", existenceOnly ? "exists" : "value");

            if (0 != (symbol.Attributes & WixRegistrySearchAttributes.Win64))
            {
                writer.WriteAttributeString("Win64", "yes");
            }

            if (!existenceOnly)
            {
                if (0 != (symbol.Attributes & WixRegistrySearchAttributes.ExpandEnvironmentVariables))
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
