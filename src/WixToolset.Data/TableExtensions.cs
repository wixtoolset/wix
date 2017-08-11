// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Methods that extend <see cref="Table"/>.
    /// </summary>
    public static class TableExtensions
    {
        /// <summary>
        /// Gets the rows contained in the table as a particular row type.
        /// </summary>
        /// <param name="table">Table to get rows from.</param>
        /// <remarks>If the <paramref name="table"/> is null, an empty enumerable will be returned.</remarks>
        public static IEnumerable<T> RowsAs<T>(this Table table) where T : Row
        {
            return (null == table) ? Enumerable.Empty<T>() : table.Rows.Cast<T>();
        }
    }
}
