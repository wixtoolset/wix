// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the RelatedPackage table.
    /// </summary>
    public class WixBundleRelatedPackageRow : Row
    {
        /// <summary>
        /// Creates a RelatedPackageRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixBundleRelatedPackageRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a RelatedPackageRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public WixBundleRelatedPackageRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the foreign key identifier to the ChainPackage row.
        /// </summary>
        public string ChainPackageId
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Id
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        public string MinVersion
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        public string MaxVersion
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public string Languages
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        public bool MinInclusive
        {
            get { return 1 == (int)this.Fields[5].Data; }
            set { this.Fields[5].Data = value ? 1 : 0; }
        }

        public bool MaxInclusive
        {
            get { return 1 == (int)this.Fields[6].Data; }
            set { this.Fields[6].Data = value ? 1 : 0; }
        }

        public bool LangInclusive
        {
            get { return 1 == (int)this.Fields[7].Data; }
            set { this.Fields[7].Data = value ? 1 : 0; }
        }

        public bool OnlyDetect
        {
            get { return 1 == (int)this.Fields[8].Data; }
            set { this.Fields[8].Data = value ? 1 : 0; }
        }
    }
}
