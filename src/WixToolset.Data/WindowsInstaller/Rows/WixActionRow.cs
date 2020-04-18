// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller.Rows
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;
    using WixToolset.Data.Tuples;

    /// <summary>
    /// Specialization of a row for the sequence tables.
    /// </summary>
    public sealed class WixActionRow : Row, IComparable
    {
        /// <summary>
        /// Instantiates an ActionRow that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Action row belongs to and should get its column definitions from.</param>
        public WixActionRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public WixActionRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition) :
            base(sourceLineNumbers, tableDefinition)
        {
        }

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        /// <value>The name of the action.</value>
        public string Action
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets the name of the action this action should be scheduled after.
        /// </summary>
        /// <value>The name of the action this action should be scheduled after.</value>
        public string After
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets the name of the action this action should be scheduled before.
        /// </summary>
        /// <value>The name of the action this action should be scheduled before.</value>
        public string Before
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the condition of the action.
        /// </summary>
        /// <value>The condition of the action.</value>
        public string Condition
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether this action is overridable.
        /// </summary>
        /// <value>Whether this action is overridable.</value>
        public bool Overridable
        {
            get { return (1 == Convert.ToInt32(this.Fields[6].Data, CultureInfo.InvariantCulture)); }
            set { this.Fields[6].Data = (value ? 1 : 0); }
        }

        /// <summary>
        /// Gets or sets the sequence number of this action.
        /// </summary>
        /// <value>The sequence number of this action.</value>
        public int Sequence
        {
            get { return Convert.ToInt32(this.Fields[3].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets of sets the sequence table of this action.
        /// </summary>
        /// <value>The sequence table of this action.</value>
        public SequenceTable SequenceTable
        {
            get { return (SequenceTable)Enum.Parse(typeof(SequenceTable), (string)this.Fields[0].Data); }
            set { this.Fields[0].Data = value.ToString(); }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">Other reference to compare this one to.</param>
        /// <returns>Returns less than 0 for less than, 0 for equals, and greater than 0 for greater.</returns>
        public int CompareTo(object obj)
        {
            WixActionRow otherActionRow = (WixActionRow)obj;

            return this.Sequence.CompareTo(otherActionRow.Sequence);
        }
    }
}
