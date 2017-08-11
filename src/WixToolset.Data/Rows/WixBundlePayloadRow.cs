// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    using System;
    using System.IO;

    /// <summary>
    /// Specialization of a row for the PayloadInfo table.
    /// </summary>
    public class WixBundlePayloadRow : Row
    {
        /// <summary>
        /// Creates a PayloadRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixBundlePayloadRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a PayloadRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public WixBundlePayloadRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Name
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        public string SourceFile
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        public string DownloadUrl
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public YesNoDefaultType Compressed
        {
            get { return (YesNoDefaultType)this.Fields[4].Data; }
            set { this.Fields[4].Data = (int)value; }
        }

        public string UnresolvedSourceFile
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        public string DisplayName
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        public string Description
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        public bool EnableSignatureValidation
        {
            get { return (null != this.Fields[8].Data) && (1 == (int)this.Fields[8].Data); }
            set { this.Fields[8].Data = value ? 1 : 0; }
        }

        public int FileSize
        {
            get { return (int)this.Fields[9].Data; }
            set { this.Fields[9].Data = value; }
        }

        public string Version
        {
            get { return (string)this.Fields[10].Data; }
            set { this.Fields[10].Data = value; }
        }

        public string Hash
        {
            get { return (string)this.Fields[11].Data; }
            set { this.Fields[11].Data = value; }
        }

        public string PublicKey
        {
            get { return (string)this.Fields[12].Data; }
            set { this.Fields[12].Data = value; }
        }

        public string Thumbprint
        {
            get { return (string)this.Fields[13].Data; }
            set { this.Fields[13].Data = value; }
        }

        public string Catalog
        {
            get { return (string)this.Fields[14].Data; }
            set { this.Fields[14].Data = value; }
        }

        public string Container
        {
            get { return (string)this.Fields[15].Data; }
            set { this.Fields[15].Data = value; }
        }

        public string Package
        {
            get { return (string)this.Fields[16].Data; }
            set { this.Fields[16].Data = value; }
        }

        public bool ContentFile
        {
            get { return (null != this.Fields[17].Data) && (1 == (int)this.Fields[17].Data); }
            set { this.Fields[17].Data = value ? 1 : 0; }
        }

        public string EmbeddedId
        {
            get { return (string)this.Fields[18].Data; }
            set { this.Fields[18].Data = value; }
        }

        public bool LayoutOnly
        {
            get { return (null != this.Fields[19].Data) && (1 == (int)this.Fields[19].Data); }
            set { this.Fields[19].Data = value ? 1 : 0; }
        }

        public PackagingType Packaging
        {
            get
            {
                object data = this.Fields[20].Data;
                return (null == data) ? PackagingType.Unknown : (PackagingType)data;
            }

            set
            {
                if (PackagingType.Unknown == value)
                {
                    this.Fields[20].Data = null;
                }
                else
                {
                    this.Fields[20].Data = (int)value;
                }
            }
        }

        public string ParentPackagePayload
        {
            get { return (string)this.Fields[21].Data; }
            set { this.Fields[21].Data = value; }
        }

        public string FullFileName
        {
            get { return String.IsNullOrEmpty(this.SourceFile) ? String.Empty : Path.GetFullPath(this.SourceFile); }
        }
    }
}
