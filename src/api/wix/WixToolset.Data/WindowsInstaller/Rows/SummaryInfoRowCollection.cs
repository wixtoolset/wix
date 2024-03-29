// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller.Rows
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Indexed container class for summary information rows.
    /// </summary>
    public sealed class SummaryInfoRowCollection : KeyedCollection<int, Row>
    {
        /// <summary>
        /// Creates the keyed collection from existing rows in a table.
        /// </summary>
        /// <param name="table">The summary information table to index.</param>
        public SummaryInfoRowCollection(Table table)
        {
            if (0 != String.CompareOrdinal("_SummaryInformation", table.Name))
            {
                throw new ArgumentException($"The table {table.Name} is not supported.", "table");
            }

            foreach (Row row in table.Rows)
            {
                this.Add(row);
            }
        }

        /// <summary>
        /// Gets the summary property ID for the <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row to index.</param>
        /// <returns>The summary property ID for the <paramref name="row"/>.</returns>
        protected override int GetKeyForItem(Row row)
        {
            return (int)row[0];
        }
    }
}
