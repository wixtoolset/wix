// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the Media table.
    /// </summary>
    public sealed class MediaRow : Row
    {
        /// <summary>
        /// Creates a Media row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public MediaRow(SourceLineNumber sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the disk id for this media row.
        /// </summary>
        /// <value>Disk id.</value>
        public int DiskId
        {
            get { return (int)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the last sequence number for this media row.
        /// </summary>
        /// <value>Last sequence number.</value>
        public int LastSequence
        {
            get { return (int)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the disk prompt for this media row.
        /// </summary>
        /// <value>Disk prompt.</value>
        public string DiskPrompt
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the cabinet name for this media row.
        /// </summary>
        /// <value>Cabinet name.</value>
        public string Cabinet
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the volume label for this media row.
        /// </summary>
        /// <value>Volume label.</value>
        public string VolumeLabel
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source for this media row.
        /// </summary>
        /// <value>Source.</value>
        public string Source
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }
    }
}
