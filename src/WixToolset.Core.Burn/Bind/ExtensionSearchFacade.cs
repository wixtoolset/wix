// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;
    using WixToolset.Data.Tuples;

    internal class ExtensionSearchFacade : BaseSearchFacade
    {
        public ExtensionSearchFacade(WixSearchTuple searchTuple)
        {
            this.SearchTuple = searchTuple;
        }

        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("ExtensionSearch");

            base.WriteXml(writer);

            writer.WriteEndElement();
        }
    }
}
