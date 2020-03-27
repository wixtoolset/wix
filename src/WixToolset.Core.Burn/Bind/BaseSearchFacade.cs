// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Xml;
    using WixToolset.Data.Tuples;

    internal abstract class BaseSearchFacade : ISearchFacade
    {
        protected WixSearchTuple SearchTuple { get; set; }

        public virtual void WriteXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Id", this.SearchTuple.Id.Id);
            writer.WriteAttributeString("Variable", this.SearchTuple.Variable);
            if (!String.IsNullOrEmpty(this.SearchTuple.Condition))
            {
                writer.WriteAttributeString("Condition", this.SearchTuple.Condition);
            }
            if (!String.IsNullOrEmpty(this.SearchTuple.BundleExtensionRef))
            {
                writer.WriteAttributeString("ExtensionId", this.SearchTuple.BundleExtensionRef);
            }
        }
    }
}
