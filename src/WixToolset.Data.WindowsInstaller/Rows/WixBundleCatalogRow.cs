// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixCatalog table.
    /// </summary>
    public sealed class WixBundleCatalogRow : Row
    {
        /// <summary>
        /// Creates a Catalog row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Catalog row belongs to and should get its column definitions from.</param>
        public WixBundleCatalogRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a Catalog row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Catalog row belongs to and should get its column definitions from.</param>
        public WixBundleCatalogRow(SourceLineNumber sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the catalog identifier.
        /// </summary>
        /// <value>The catalog identifier.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the payload identifier.
        /// </summary>
        /// <value>The payload identifier.</value>
        public string Payload
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }
    }
}
