// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Xml;
    using WixToolset.Data.Symbols;

    internal abstract class BaseSearchFacade : ISearchFacade
    {
        protected WixSearchSymbol SearchSymbol { get; set; }

        public virtual void WriteXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Id", this.SearchSymbol.Id.Id);
            writer.WriteAttributeString("Variable", this.SearchSymbol.Variable);
            if (!String.IsNullOrEmpty(this.SearchSymbol.Condition))
            {
                writer.WriteAttributeString("Condition", this.SearchSymbol.Condition);
            }
            if (!String.IsNullOrEmpty(this.SearchSymbol.BundleExtensionRef))
            {
                writer.WriteAttributeString("ExtensionId", this.SearchSymbol.BundleExtensionRef);
            }
        }
    }
}
