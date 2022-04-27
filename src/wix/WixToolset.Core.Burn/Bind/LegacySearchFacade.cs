// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Diagnostics;
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
                default:
                    throw new NotImplementedException();
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

            switch (searchSymbol.Type)
            {
                case WixComponentSearchType.KeyPath:
                    writer.WriteAttributeString("Type", "keyPath");
                    break;
                case WixComponentSearchType.State:
                    writer.WriteAttributeString("Type", "state");
                    break;
                case WixComponentSearchType.WantDirectory:
                    writer.WriteAttributeString("Type", "directory");
                    break;
                default:
                    throw new NotImplementedException();
            }

            writer.WriteEndElement();
        }

        private void WriteFileSearchXml(XmlTextWriter writer, WixFileSearchSymbol searchSymbol)
        {
            writer.WriteStartElement(!searchSymbol.IsDirectory ? "FileSearch" : "DirectorySearch");

            base.WriteXml(writer);

            writer.WriteAttributeString("Path", searchSymbol.Path);

            switch (searchSymbol.Type)
            {
                case WixFileSearchType.Exists:
                    writer.WriteAttributeString("Type", "exists");
                    break;
                case WixFileSearchType.Version:
                    Debug.Assert(!searchSymbol.IsDirectory, "Version search type is invalid for DirectorySearch");
                    writer.WriteAttributeString("Type", "version");
                    break;
                case WixFileSearchType.Path:
                    writer.WriteAttributeString("Type", "path");
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (searchSymbol.DisableFileRedirection)
            {
                writer.WriteAttributeString("DisableFileRedirection", "yes");
            }

            writer.WriteEndElement();
        }

        private void WriteProductSearchXml(XmlTextWriter writer, WixProductSearchSymbol symbol)
        {
            writer.WriteStartElement("MsiProductSearch");

            base.WriteXml(writer);

            if (symbol.IsUpgradeCode)
            {
                writer.WriteAttributeString("UpgradeCode", symbol.Guid);
            }
            else
            {
                writer.WriteAttributeString("ProductCode", symbol.Guid);
            }

            switch (symbol.Type)
            {
                case WixProductSearchType.Version:
                    writer.WriteAttributeString("Type", "version");
                    break;
                case WixProductSearchType.Language:
                    writer.WriteAttributeString("Type", "language");
                    break;
                case WixProductSearchType.State:
                    writer.WriteAttributeString("Type", "state");
                    break;
                case WixProductSearchType.Assignment:
                    writer.WriteAttributeString("Type", "assignment");
                    break;
                default:
                    throw new NotImplementedException();
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
                default:
                    throw new NotImplementedException();
            }

            writer.WriteAttributeString("Key", symbol.Key);

            if (!String.IsNullOrEmpty(symbol.Value))
            {
                writer.WriteAttributeString("Value", symbol.Value);
            }

            if (symbol.Win64)
            {
                writer.WriteAttributeString("Win64", "yes");
            }

            switch (symbol.Type)
            {
                case WixRegistrySearchType.Exists:
                    writer.WriteAttributeString("Type", "exists");
                    break;
                case WixRegistrySearchType.Value:
                    writer.WriteAttributeString("Type", "value");

                    if (symbol.ExpandEnvironmentVariables)
                    {
                        writer.WriteAttributeString("ExpandEnvironment", "yes");
                    }

                    // We *always* say this is VariableType="string".
                    // If we end up needing to be more specific,
                    // we will have to actually implement the "Format" attribute.
                    writer.WriteAttributeString("VariableType", "string");

                    break;
                default:
                    throw new NotImplementedException();
            }

            writer.WriteEndElement();
        }
    }
}
