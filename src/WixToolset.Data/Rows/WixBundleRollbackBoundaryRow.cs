// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixBundleRollbackBoundary table.
    /// </summary>
    public sealed class WixBundleRollbackBoundaryRow : Row
    {
        /// <summary>
        /// Creates a WixBundleRollbackBoundary row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixBundleRollbackBoundaryRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a RollbackBoundaryRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixBundleRollbackBoundaryRow(SourceLineNumber sourceLineNumbers, Table table) :
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

        /// <summary>
        /// Gets or sets whether the package is vital.
        /// </summary>
        /// <value>Vitality of the package.</value>
        public YesNoType Vital
        {
            get { return (null == this.Fields[1].Data) ? YesNoType.NotSet : (YesNoType)this.Fields[1].Data; }
            set { this.Fields[1].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets whether the rollback-boundary should be installed as an MSI transaction.
        /// </summary>
        /// <value>Vitality of the package.</value>
        public YesNoType Transaction
        {
            get { return (null == this.Fields[2].Data) ? YesNoType.NotSet : (YesNoType)this.Fields[2].Data; }
            set { this.Fields[2].Data = (int)value; }
        }
    }
}
