// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Collection for table definitions indexed by table name.
    /// </summary>
    public sealed class TableDefinitionCollection : ICollection<TableDefinition>
    {
        public const string XmlNamespaceUri = "http://wixtoolset.org/schemas/v4/wi/tables";

        private readonly Dictionary<string, TableDefinition> collection;

        /// <summary>
        /// Instantiate a new TableDefinitionCollection class.
        /// </summary>
        public TableDefinitionCollection()
        {
            this.collection = new Dictionary<string, TableDefinition>();
        }

        /// <summary>
        /// Creates a shallow copy of the provided table definition collection.
        /// </summary>
        public TableDefinitionCollection(TableDefinitionCollection tableDefinitions)
        {
            this.collection = new Dictionary<string, TableDefinition>(tableDefinitions.collection);
        }

        /// <summary>
        /// Creates a table definition collection with the given table definitions.
        /// </summary>
        public TableDefinitionCollection(IEnumerable<TableDefinition> tableDefinitions)
        {
            this.collection = tableDefinitions.ToDictionary(t => t.Name);
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>Number of items in collection.</value>
        public int Count => this.collection.Count;

        /// <summary>
        /// Table definition collections are never read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a table definition by name.
        /// </summary>
        /// <param name="tableName">Name of table to locate.</param>
        public TableDefinition this[string tableName]
        {
            get
            {
                if (!this.collection.TryGetValue(tableName, out var table))
                {
                    throw new WixMissingTableDefinitionException(ErrorMessages.MissingTableDefinition(tableName));
                }

                return table;
            }
        }

        /// <summary>
        /// Tries to get a table definition by name.
        /// </summary>
        /// <param name="tableName">Name of table to locate.</param>
        /// <param name="table">Table definition if found.</param>
        /// <returns>True if table definition was found otherwise false.</returns>
        public bool TryGet(string tableName, out TableDefinition table)
        {
            return this.collection.TryGetValue(tableName, out table);
        }

        /// <summary>
        /// Adds a table definition to the collection.
        /// </summary>
        /// <param name="tableDefinition">Table definition to add to the collection.</param>
        /// <value>Indexes by table definition name.</value>
        public void Add(TableDefinition tableDefinition)
        {
            this.collection.Add(tableDefinition.Name, tableDefinition);
        }

        /// <summary>
        /// Removes all table definitions from the collection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// Checks if the collection contains a table name.
        /// </summary>
        /// <param name="tableName">The table to check in the collection.</param>
        /// <returns>True if collection contains the table.</returns>
        public bool Contains(string tableName)
        {
            return this.collection.ContainsKey(tableName);
        }

        /// <summary>
        /// Checks if the collection contains a table.
        /// </summary>
        /// <param name="table">The table to check in the collection.</param>
        /// <returns>True if collection contains the table.</returns>
        public bool Contains(TableDefinition table)
        {
            return this.collection.ContainsKey(table.Name);
        }

        /// <summary>
        /// Copies table definitions to an arry.
        /// </summary>
        /// <param name="array">Array to copy the table definitions to.</param>
        /// <param name="index">Index in the array to start copying at.</param>
        public void CopyTo(TableDefinition[] array, int index)
        {
            this.collection.Values.CopyTo(array, index);
        }

        /// <summary>
        /// Removes a table definition from the collection.
        /// </summary>
        /// <param name="table">Table to remove from the collection.</param>
        /// <returns>True if the table definition existed in the collection and was removed.</returns>
        public bool Remove(TableDefinition table)
        {
            return this.collection.Remove(table.Name);
        }

        /// <summary>
        /// Gets enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        public IEnumerator<TableDefinition> GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Gets the untyped enumerator for the collection.
        /// </summary>
        /// <returns>Untyped enumerator for the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Loads a collection of table definitions from a XmlReader in memory.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <param name="tableDefinitions">Table definitions to use for strongly-typed rows.</param>
        /// <returns>The TableDefinitionCollection represented by the xml.</returns>
        internal static TableDefinitionCollection Read(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            if ("tableDefinitions" != reader.LocalName)
            {
                throw new XmlException();
            }

            var empty = reader.IsEmptyElement;
            var tableDefinitionCollection = new TableDefinitionCollection();

            while (reader.MoveToNextAttribute())
            {
            }

            // parse the child elements
            if (!empty)
            {
                var done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "tableDefinition":
                                    tableDefinitionCollection.Add(TableDefinition.Read(reader, tableDefinitions));
                                    break;
                                default:
                                    throw new XmlException();
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new XmlException();
                }
            }

            return tableDefinitionCollection;
        }

        /// <summary>
        /// Persists a TableDefinitionCollection in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the TableDefinitionCollection should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("tableDefinitions", XmlNamespaceUri);

            foreach (var tableDefinition in this.collection.Values.OrderBy(t => t.Name))
            {
                tableDefinition.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
