// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;
    using WixToolset.Data.Symbols;

    internal class SetVariableSearchFacade : BaseSearchFacade
    {
        public SetVariableSearchFacade(WixSearchSymbol searchSymbol, WixSetVariableSymbol setVariableSymbol)
        {
            this.SearchSymbol = searchSymbol;
            this.SetVariableSymbol = setVariableSymbol;
        }

        private WixSetVariableSymbol SetVariableSymbol { get; }

        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("SetVariable");

            base.WriteXml(writer);

            if (this.SetVariableSymbol.Type != WixBundleVariableType.Unknown)
            {
                writer.WriteAttributeString("Value", this.SetVariableSymbol.Value);

                switch (this.SetVariableSymbol.Type)
                {
                    case WixBundleVariableType.Formatted:
                        writer.WriteAttributeString("Type", "formatted");
                        break;
                    case WixBundleVariableType.Numeric:
                        writer.WriteAttributeString("Type", "numeric");
                        break;
                    case WixBundleVariableType.String:
                        writer.WriteAttributeString("Type", "string");
                        break;
                    case WixBundleVariableType.Version:
                        writer.WriteAttributeString("Type", "version");
                        break;
                }
            }

            writer.WriteEndElement();
        }
    }
}
