// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    /// Object that represents a table in a database.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class Table
    {
        /// <summary>
        /// Creates a table.
        /// </summary>
        /// <param name="tableDefinition">Definition of the table.</param>
        public Table(TableDefinition tableDefinition)
        {
            this.Definition = tableDefinition;
            this.Rows = new List<Row>();
        }

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        /// <value>Definition of the table.</value>
        public TableDefinition Definition { get; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        public string Name => this.Definition.Name;

        /// <summary>
        /// Gets or sets the table transform operation.
        /// </summary>
        /// <value>The table transform operation.</value>
        public TableOperation Operation { get; set; }

        /// <summary>
        /// Gets the rows contained in the table.
        /// </summary>
        /// <value>Rows contained in the table.</value>
        public IList<Row> Rows { get; }

        /// <summary>
        /// Creates a new row and adds it to the table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <returns>Row created in table.</returns>
        public Row CreateRow(SourceLineNumber sourceLineNumbers)
        {
            var row = this.Definition.CreateRow(sourceLineNumbers, this);
            this.Rows.Add(row);
            return row;
        }

        /// <summary>
        /// Validates the rows of this OutputTable and throws if it collides on
        /// primary keys.
        /// </summary>
        public void ValidateRows()
        {
            var primaryKeys = new Dictionary<string, SourceLineNumber>();

            foreach (var row in this.Rows)
            {
                var primaryKey = row.GetPrimaryKey();

                if (primaryKeys.TryGetValue(primaryKey, out var collisionSourceLineNumber))
                {
                    throw new WixException(ErrorMessages.DuplicatePrimaryKey(collisionSourceLineNumber, primaryKey, this.Definition.Name));
                }

                primaryKeys.Add(primaryKey, row.SourceLineNumbers);
            }
        }

        /// <summary>
        /// Parse a table from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <returns>The parsed table.</returns>
        internal static Table Read(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            Debug.Assert("table" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            TableOperation operation = TableOperation.None;
            string name = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                        name = reader.Value;
                        break;
                    case "op":
                        switch (reader.Value)
                        {
                            case "add":
                                operation = TableOperation.Add;
                                break;
                            case "drop":
                                operation = TableOperation.Drop;
                                break;
                            default:
                                throw new XmlException();
                        }
                        break;
                }
            }

            if (null == name)
            {
                throw new XmlException();
            }

            TableDefinition tableDefinition = tableDefinitions[name];
            Table table = new Table(tableDefinition);
            table.Operation = operation;

            if (!empty)
            {
                bool done = false;

                // loop through all the rows in a table
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "row":
                                    Row.Read(reader, table);
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

            return table;
        }

        /// <summary>
        /// Persists a row in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            if (null == writer)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("table", WindowsInstallerData.XmlNamespaceUri);
            writer.WriteAttributeString("name", this.Name);

            if (TableOperation.None != this.Operation)
            {
                writer.WriteAttributeString("op", this.Operation.ToString().ToLowerInvariant());
            }

            foreach (var row in this.Rows)
            {
                row.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
