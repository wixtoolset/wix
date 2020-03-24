// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller.Rows
{
    /// <summary>
    /// Specialization of a row for the Property table.
    /// </summary>
    public sealed class PropertyRow : Row
    {
        /// <summary>
        /// Creates an Property row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Upgrade row belongs to and should get its column definitions from.</param>
        public PropertyRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets and sets the property name for the row.
        /// </summary>
        /// <value>Property identifier for the row.</value>
        public string Property
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets and sets the property value for the row.
        /// </summary>
        /// <value>Property value for the row.</value>
        public string Value
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }
    }
}
