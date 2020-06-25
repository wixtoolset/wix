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

            if (this.SetVariableSymbol.Type != null)
            {
                writer.WriteAttributeString("Value", this.SetVariableSymbol.Value);
                writer.WriteAttributeString("Type", this.SetVariableSymbol.Type);
            }

            writer.WriteEndElement();
        }
    }
}
