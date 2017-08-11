// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the Container table.
    /// </summary>
    public class WixBundleContainerRow : Row
    {
        /// <summary>
        /// Creates a ContainerRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixBundleContainerRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a ContainerRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public WixBundleContainerRow(SourceLineNumber sourceLineNumbers, Table table) :
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

        public ContainerType Type
        {
            get { return (ContainerType)this.Fields[2].Data; }
            set { this.Fields[2].Data = (int)value; }
        }

        public string DownloadUrl
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public long Size
        {
            get { return (long)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        public string Hash
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        public int AttachedContainerIndex
        {
            get { return (null == this.Fields[6].Data) ? -1 : (int)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        public string WorkingPath
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }
    }
}
