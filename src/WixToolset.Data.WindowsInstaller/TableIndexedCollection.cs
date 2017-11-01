// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Collection for tables.
    /// </summary>
    public sealed class TableIndexedCollection : ICollection<Table>
    {
        private Dictionary<string, Table> collection;

        /// <summary>
        /// Instantiate a new empty collection.
        /// </summary>
        public TableIndexedCollection()
        {
            this.collection = new Dictionary<string,Table>();
        }

        /// <summary>
        /// Instantiate a new collection populated with a set of tables.
        /// </summary>
        /// <param name="tables">Set of tables.</param>
        public TableIndexedCollection(IEnumerable<Table> tables)
        {
            this.collection = tables.ToDictionary(t => t.Name);
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>Number of items in collection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Table indexed collection is never read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds a table to the collection.
        /// </summary>
        /// <param name="table">Table to add to the collection.</param>
        /// <remarks>Indexes the table by name.</remarks>
        public void Add(Table table)
        {
            this.collection.Add(table.Name, table);
        }

        /// <summary>
        /// Clear the tables from the collection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// Determines if a table is in the collection.
        /// </summary>
        /// <param name="table">Table to check if it is in the collection.</param>
        /// <returns>True if the table name is in the collection, otherwise false.</returns>
        public bool Contains(Table table)
        {
            return this.collection.ContainsKey(table.Name);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="arrayIndex">Index to start copying from.</param>
        public void CopyTo(Table[] array, int arrayIndex)
        {
            this.collection.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Remove a table from the collection by name.
        /// </summary>
        /// <param name="tableName">Table name to remove from the collection.</param>
        public void Remove(string tableName)
        {
            this.collection.Remove(tableName);
        }

        /// <summary>
        /// Remove a table from the collection.
        /// </summary>
        /// <param name="table">Table with matching name to remove from the collection.</param>
        public bool Remove(Table table)
        {
            return this.collection.Remove(table.Name);
        }

        /// <summary>
        /// Gets an enumerator over the whole collection.
        /// </summary>
        /// <returns>Collection enumerator.</returns>
        public IEnumerator<Table> GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Gets an untyped enumerator over the whole collection.
        /// </summary>
        /// <returns>Untyped collection enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Gets a table by name.
        /// </summary>
        /// <param name="tableName">Name of table to locate.</param>
        public Table this[string tableName]
        {
            get
            {
                Table table;
                return this.collection.TryGetValue(tableName, out table) ? table : null;
            }

            set
            {
                this.collection[tableName] = value;
            }
        }

        /// <summary>
        /// Tries to find a table by name.
        /// </summary>
        /// <param name="tableName">Table name to locate.</param>
        /// <param name="table">Found table.</param>
        /// <returns>True if table with table name was found, otherwise false.</returns>
        public bool TryGetTable(string tableName, out Table table)
        {
            return this.collection.TryGetValue(tableName, out table);
        }
    }
}
