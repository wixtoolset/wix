// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;
    using WixToolset.Data.Symbols;

    internal class ExtensionSearchFacade : BaseSearchFacade
    {
        public ExtensionSearchFacade(WixSearchSymbol searchSymbol)
        {
            this.SearchSymbol = searchSymbol;
        }

        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("ExtensionSearch");

            base.WriteXml(writer);

            writer.WriteEndElement();
        }
    }
}
