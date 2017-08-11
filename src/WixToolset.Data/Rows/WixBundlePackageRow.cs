// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixBundlePackage table.
    /// </summary>
    public sealed class WixBundlePackageRow : Row
    {
        /// <summary>
        /// Creates a WixBundlePackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixBundlePackageRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixBundlePackage row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixBundlePackageRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the foreign key to the WixChainItem.
        /// </summary>
        public string WixChainItemId
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the item type.
        /// </summary>
        public WixBundlePackageType Type
        {
            get { return (WixBundlePackageType)this.Fields[1].Data; }
            set { this.Fields[1].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the indentifier of the package's payload.
        /// </summary>
        public string PackagePayload
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the raw attributes of a package.
        /// </summary>
        public WixBundlePackageAttributes Attributes
        {
            get { return (WixBundlePackageAttributes)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the install condition of the package.
        /// </summary>
        public string InstallCondition
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the language of the package.
        /// </summary>
        public YesNoAlwaysType Cache
        {
            get { return (null == this.Fields[5].Data) ? YesNoAlwaysType.NotSet : (YesNoAlwaysType)this.Fields[5].Data; }
            set { this.Fields[5].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the indentifier of the package's cache.
        /// </summary>
        public string CacheId
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether the package is vital.
        /// </summary>
        public YesNoType Vital
        {
            get { return (null == this.Fields[7].Data) ? YesNoType.NotSet : (YesNoType)this.Fields[7].Data; }
            set { this.Fields[7].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets whether the package is per-machine.
        /// </summary>
        public YesNoDefaultType PerMachine
        {
            get { return (null == this.Fields[8].Data) ? YesNoDefaultType.NotSet : (YesNoDefaultType)this.Fields[8].Data; }
            set { this.Fields[8].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the variable that points to the log for the package.
        /// </summary>
        public string LogPathVariable
        {
            get { return (string)this.Fields[9].Data; }
            set { this.Fields[9].Data = value; }
        }

        /// <summary>
        /// Gets or sets the variable that points to the rollback log for the package.
        /// </summary>
        public string RollbackLogPathVariable
        {
            get { return (string)this.Fields[10].Data; }
            set { this.Fields[10].Data = value; }
        }

        /// <summary>
        /// Gets or sets the size of the package.
        /// </summary>
        public long Size
        {
            get { return (long)this.Fields[11].Data; }
            set { this.Fields[11].Data = value; }
        }

        /// <summary>
        /// Gets or sets the install size of the package.
        /// </summary>
        public long? InstallSize
        {
            get { return (long?)this.Fields[12].Data; }
            set { this.Fields[12].Data = value; }
        }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        public string Version
        {
            get { return (string)this.Fields[13].Data; }
            set { this.Fields[13].Data = value; }
        }

        /// <summary>
        /// Gets or sets the language of the package.
        /// </summary>
        public int Language
        {
            get { return (int)this.Fields[14].Data; }
            set { this.Fields[14].Data = value; }
        }

        /// <summary>
        /// Gets or sets the display name of the package.
        /// </summary>
        public string DisplayName
        {
            get { return (string)this.Fields[15].Data; }
            set { this.Fields[15].Data = value; }
        }

        /// <summary>
        /// Gets or sets the description of the package.
        /// </summary>
        public string Description
        {
            get { return (string)this.Fields[16].Data; }
            set { this.Fields[16].Data = value; }
        }

        /// <summary>
        /// Gets or sets the rollback boundary identifier for the package.
        /// </summary>
        public string RollbackBoundary
        {
            get { return (string)this.Fields[17].Data; }
            set { this.Fields[17].Data = value; }
        }

        /// <summary>
        /// Gets or sets the backward rollback boundary identifier for the package.
        /// </summary>
        public string RollbackBoundaryBackward
        {
            get { return (string)this.Fields[18].Data; }
            set { this.Fields[18].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether the package is x64.
        /// </summary>
        public YesNoType x64
        {
            get { return (null == this.Fields[19].Data) ? YesNoType.NotSet : (YesNoType)this.Fields[19].Data; }
            set { this.Fields[19].Data = (int)value; }
        }

        /// <summary>
        /// Gets whether the package is permanent.
        /// </summary>
        public bool Permanent
        {
            get { return 0 != (this.Attributes & WixBundlePackageAttributes.Permanent); }
        }

        /// <summary>
        /// Gets whether the package is visible.
        /// </summary>
        public bool Visible
        {
            get { return 0 != (this.Attributes & WixBundlePackageAttributes.Visible); }
        }
    }
}
