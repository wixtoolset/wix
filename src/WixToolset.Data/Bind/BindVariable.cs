// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixVariable table.
    /// </summary>
    public sealed class BindVariable
    {
        /// <summary>
        /// Gets or sets the source line number.
        /// </summary>
        public SourceLineNumber SourceLineNumbers { get; set; }

        /// <summary>
        /// Gets or sets the variable identifier.
        /// </summary>
        /// <value>The variable identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the variable's value.
        /// </summary>
        /// <value>The variable's value.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets whether this variable is overridable.
        /// </summary>
        /// <value>Whether this variable is overridable.</value>
        public bool Overridable { get; set; }
    }
}
