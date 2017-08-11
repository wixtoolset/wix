// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixDeltaPatchFile table.
    /// </summary>
    public sealed class WixDeltaPatchFileRow : Row
    {
        /// <summary>
        /// Creates a WixDeltaPatchFile row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixDeltaPatchFileRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixDeltaPatchFile row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this File row belongs to and should get its column definitions from.</param>
        public WixDeltaPatchFileRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the primary key of the file row.
        /// </summary>
        /// <value>Primary key of the file row.</value>
        public string File
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-length list for the file.
        /// </summary>
        /// <value>RetainLength list for the file.</value>
        public string RetainLengths
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch retain-length list for the file.
        /// </summary>
        /// <value>Previous RetainLength list for the file.</value>
        public string PreviousRetainLengths
        {
            get { return this.Fields[1].PreviousData; }
            set { this.Fields[1].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-offset list for the file.
        /// </summary>
        /// <value>IgnoreOffset list for the file.</value>
        public string IgnoreOffsets
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch ignore-offset list for the file.
        /// </summary>
        /// <value>Previous IgnoreOffset list for the file.</value>
        public string PreviousIgnoreOffsets
        {
            get { return this.Fields[2].PreviousData; }
            set { this.Fields[2].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-length list for the file.
        /// </summary>
        /// <value>IgnoreLength list for the file.</value>
        public string IgnoreLengths
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch ignore-length list for the file.
        /// </summary>
        /// <value>Previous IgnoreLength list for the file.</value>
        public string PreviousIgnoreLengths
        {
            get { return this.Fields[3].PreviousData; }
            set { this.Fields[3].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-offset list for the file.
        /// </summary>
        /// <value>RetainOffset list for the file.</value>
        public string RetainOffsets
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch retain-offset list for the file.
        /// </summary>
        /// <value>PreviousRetainOffset list for the file.</value>
        public string PreviousRetainOffsets
        {
            get { return this.Fields[4].PreviousData; }
            set { this.Fields[4].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the symbol paths for the file.
        /// </summary>
        /// <value>SymbolPath list for the file.</value>
        /// <remarks>This is set during binding.</remarks>
        public string Symbols
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous symbol paths for the file.
        /// </summary>
        /// <value>PreviousSymbolPath list for the file.</value>
        /// <remarks>This is set during binding.</remarks>
        public string PreviousSymbols
        {
            get { return (string)this.Fields[5].PreviousData; }
            set { this.Fields[5].PreviousData = value; }
        }
    }
}
