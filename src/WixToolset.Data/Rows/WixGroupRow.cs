// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    using System;

    /// <summary>
    /// Specialization of a row for the WixGroup table.
    /// </summary>
    public sealed class WixGroupRow : Row
    {
        /// <summary>
        /// Creates a WixGroupRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixGroupRow(SourceLineNumber sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the parent identifier of the complex reference.
        /// </summary>
        /// <value>Parent identifier of the complex reference.</value>
        public string ParentId
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets the parent type of the complex reference.
        /// </summary>
        /// <value>Parent type of the complex reference.</value>
        public ComplexReferenceParentType ParentType
        {
            get { return (ComplexReferenceParentType)Enum.Parse(typeof(ComplexReferenceParentType), (string)this.Fields[1].Data); }
            set { this.Fields[1].Data = value.ToString(); }
        }

        /// <summary>
        /// Gets the child identifier of the complex reference.
        /// </summary>
        /// <value>Child identifier of the complex reference.</value>
        public string ChildId
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets the child type of the complex reference.
        /// </summary>
        /// <value>Child type of the complex reference.</value>
        public ComplexReferenceChildType ChildType
        {
            get { return (ComplexReferenceChildType)Enum.Parse(typeof(ComplexReferenceChildType), (string)this.Fields[3].Data); }
            set { this.Fields[3].Data = value.ToString(); }
        }
    }
}
