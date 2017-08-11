// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the ChainMspPackage table.
    /// </summary>
    public sealed class WixBundleMspPackageRow : Row
    {
        /// <summary>
        /// Creates a ChainMspPackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixBundleMspPackageRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixBundleMspPackage row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixBundleMspPackageRow(SourceLineNumber sourceLineNumbers, Table table) :
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
        /// Gets or sets the raw MSP attributes of a patch.
        /// </summary>
        public WixBundleMspPackageAttributes Attributes
        {
            get { return (WixBundleMspPackageAttributes)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the patch code.
        /// </summary>
        public string PatchCode
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the patch's manufacturer.
        /// </summary>
        public string Manufacturer
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the patch's xml.
        /// </summary>
        public string PatchXml
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets the display internal UI of a patch.
        /// </summary>
        public bool DisplayInternalUI
        {
            get { return 0 != (this.Attributes & WixBundleMspPackageAttributes.DisplayInternalUI); }
        }

        /// <summary>
        /// Gets whether to slipstream the patch.
        /// </summary>
        public bool Slipstream
        {
            get { return 0 != (this.Attributes & WixBundleMspPackageAttributes.Slipstream); }
        }

        /// <summary>
        /// Gets whether the patch targets an unspecified number of packages.
        /// </summary>
        public bool TargetUnspecified
        {
            get { return 0 != (this.Attributes & WixBundleMspPackageAttributes.TargetUnspecified); }
        }
    }
}
