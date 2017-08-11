// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A list of rows indexed by their primary key. Unlike a <see cref="RowDictionary"/>
    /// this indexed list will track rows in their added order and will allow rows with
    /// duplicate keys to be added to the list, although only the first row will be indexed.
    /// </summary>
    public sealed class RowIndexedList<T> : IList<T> where T : Row
    {
        private Dictionary<string, T> index;
        private List<T> rows;
        private List<T> duplicates;

        /// <summary>
        /// Creates an empty <see cref="RowIndexedList"/>.
        /// </summary>
        public RowIndexedList()
        {
            this.index = new Dictionary<string, T>(StringComparer.InvariantCulture);
            this.rows = new List<T>();
            this.duplicates = new List<T>();
        }

        /// <summary>
        /// Creates and populates a <see cref="RowDictionary"/> with the rows from the given enumerator.
        /// </summary>
        /// <param name="rows">Rows to index.</param>
        public RowIndexedList(IEnumerable<T> rows)
            : this()
        {
            foreach (T row in rows)
            {
                this.Add(row);
            }
        }

        /// <summary>
        /// Creates and populates a <see cref="RowDictionary"/> with the rows from the given <see cref="Table"/>.
        /// </summary>
        /// <param name="table">The table to index.</param>
        /// <remarks>
        /// Rows added to the index are not automatically added to the given <paramref name="table"/>.
        /// </remarks>
        public RowIndexedList(Table table)
            : this()
        {
            if (null != table)
            {
                foreach (T row in table.Rows)
                {
                    this.Add(row);
                }
            }
        }

        /// <summary>
        /// Gets the duplicates in the list.
        /// </summary>
        public IEnumerable<T> Duplicates { get { return this.duplicates; } }

        /// <summary>
        /// Gets the row by integer key.
        /// </summary>
        /// <param name="key">Integer key to look up.</param>
        /// <returns>Row or null if key is not found.</returns>
        public T Get(int key)
        {
            return this.Get(key.ToString());
        }

        /// <summary>
        /// Gets the row by string key.
        /// </summary>
        /// <param name="key">String key to look up.</param>
        /// <returns>Row or null if key is not found.</returns>
        public T Get(string key)
        {
            T result;
            return this.TryGet(key, out result) ? result : null;
        }

        /// <summary>
        /// Gets the row by string key if it exists.
        /// </summary>
        /// <param name="key">Key of row to get.</param>
        /// <param name="row">Row found.</param>
        /// <returns>True if key was found otherwise false.</returns>
        public bool TryGet(string key, out T row)
        {
            return this.index.TryGetValue(key, out row);
        }

        /// <summary>
        /// Tries to add a row as long as it would not create a duplicate.
        /// </summary>
        /// <param name="row">Row to add.</param>
        /// <returns>True if the row as added otherwise false.</returns>
        public bool TryAdd(T row)
        {
            try
            {
                this.index.Add(row.GetKey(), row);
            }
            catch (ArgumentException) // if the key already exists, bail.
            {
                return false;
            }

            this.rows.Add(row);
            return true;
        }

        /// <summary>
        /// Adds a row to the list. If a row with the same key is already index, the row is
        /// is not in the index but will still be part of the list and added to the duplicates
        /// list.
        /// </summary>
        /// <param name="row"></param>
        public void Add(T row)
        {
            this.rows.Add(row);
            try
            {
                this.index.Add(row.GetKey(), row);
            }
            catch (ArgumentException) // if the key already exists, we have a duplicate.
            {
                this.duplicates.Add(row);
            }
        }

        /// <summary>
        /// Gets the index of a row.
        /// </summary>
        /// <param name="row">Iterates through the list of rows to find the index of a particular row.</param>
        /// <returns>Index of row or -1 if not found.</returns>
        public int IndexOf(T row)
        {
            return this.rows.IndexOf(row);
        }

        /// <summary>
        /// Inserts a row at a particular index of the list.
        /// </summary>
        /// <param name="index">Index to insert the row after.</param>
        /// <param name="row">Row to insert.</param>
        public void Insert(int index, T row)
        {
            this.rows.Insert(index, row);
            try
            {
                this.index.Add(row.GetKey(), row);
            }
            catch (ArgumentException) // if the key already exists, we have a duplicate.
            {
                this.duplicates.Add(row);
            }
        }

        /// <summary>
        /// Removes a row from a particular index.
        /// </summary>
        /// <param name="index">Index to remove the row at.</param>
        public void RemoveAt(int index)
        {
            T row = this.rows[index];

            this.rows.RemoveAt(index);

            T indexRow;
            if (this.index.TryGetValue(row.GetKey(), out indexRow) && indexRow == row)
            {
                this.index.Remove(row.GetKey());
            }
            else // only try to remove from duplicates if the row was not indexed (if it was indexed, it wasn't a dupe).
            {
                this.duplicates.Remove(row);
            }
        }

        /// <summary>
        /// Gets or sets a row at the specified index.
        /// </summary>
        /// <param name="index">Index to get the row.</param>
        /// <returns>Row at specified index.</returns>
        public T this[int index]
        {
            get
            {
                return this.rows[index];
            }
            set
            {
                this.rows[index] = value;
                try
                {
                    this.index.Add(value.GetKey(), value);
                }
                catch (ArgumentException) // if the key already exists, we have a duplicate.
                {
                    this.duplicates.Add(value);
                }
            }
        }

        /// <summary>
        /// Empties the list and it's index.
        /// </summary>
        public void Clear()
        {
            this.index.Clear();
            this.rows.Clear();
            this.duplicates.Clear();
        }

        /// <summary>
        /// Searches the list for a row without using the index.
        /// </summary>
        /// <param name="row">Row to look for in the list.</param>
        /// <returns>True if the row is in the list, otherwise false.</returns>
        public bool Contains(T row)
        {
            return this.rows.Contains(row);
        }

        /// <summary>
        /// Copies the rows of the list to an array.
        /// </summary>
        /// <param name="array">Array to copy the list into.</param>
        /// <param name="arrayIndex">Index to start copying at.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.rows.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Number of rows in the list.
        /// </summary>
        public int Count
        {
            get { return this.rows.Count; }
        }

        /// <summary>
        /// Indicates whether the list is read-only. Always false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes a row from the list. Indexed rows will be removed but the colleciton will NOT
        /// promote duplicates to the index automatically. The duplicate would also need to be removed
        /// and re-added to be indexed.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool Remove(T row)
        {
            bool removed = this.rows.Remove(row);
            if (removed)
            {
                T indexRow;
                if (this.index.TryGetValue(row.GetKey(), out indexRow) && indexRow == row)
                {
                    this.index.Remove(row.GetKey());
                }
                else // only try to remove from duplicates if the row was not indexed (if it was indexed, it wasn't a dupe).
                {
                    this.duplicates.Remove(row);
                }
            }

            return removed;
        }

        /// <summary>
        /// Gets an enumerator over the whole list.
        /// </summary>
        /// <returns>List enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return this.rows.GetEnumerator();
        }

        /// <summary>
        /// Gets an untyped enumerator over the whole list.
        /// </summary>
        /// <returns>Untyped list enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.rows.GetEnumerator();
        }
    }
}
