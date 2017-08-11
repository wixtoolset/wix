// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixDeltaPatchSymbolPaths table.
    /// </summary>
    public sealed class WixDeltaPatchSymbolPathsRow : Row
    {
        /// <summary>
        /// Creates a WixDeltaPatchSymbolPaths row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixDeltaPatchSymbolPathsRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixDeltaPatchSymbolPaths row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixDeltaPatchSymbolPathsRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the identifier the symbol paths apply to.
        /// </summary>
        /// <value>RetainLength list for the file.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the type of the identifier.
        /// </summary>
        public SymbolPathType Type
        {
            get { return (SymbolPathType)this.Fields[1].AsInteger(); }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch symbol paths.
        /// </summary>
        public string SymbolPaths
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }
    }
}
