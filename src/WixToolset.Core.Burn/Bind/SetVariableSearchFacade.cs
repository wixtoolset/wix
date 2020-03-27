// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;
    using WixToolset.Data.Tuples;

    internal class SetVariableSearchFacade : BaseSearchFacade
    {
        public SetVariableSearchFacade(WixSearchTuple searchTuple, WixSetVariableTuple setVariableTuple)
        {
            this.SearchTuple = searchTuple;
            this.SetVariableTuple = setVariableTuple;
        }

        private WixSetVariableTuple SetVariableTuple { get; }

        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("SetVariable");

            base.WriteXml(writer);

            if (this.SetVariableTuple.Type != null)
            {
                writer.WriteAttributeString("Value", this.SetVariableTuple.Value);
                writer.WriteAttributeString("Type", this.SetVariableTuple.Type);
            }

            writer.WriteEndElement();
        }
    }
}
