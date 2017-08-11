// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Definition of a table in a database.
    /// </summary>
    public sealed class TableDefinition : IComparable<TableDefinition>
    {
        /// <summary>
        /// Tracks the maximum number of columns supported in a real table.
        /// This is a Windows Installer limitation.
        /// </summary>
        public const int MaxColumnsInRealTable = 32;

        /// <summary>
        /// Creates a table definition.
        /// </summary>
        /// <param name="name">Name of table to create.</param>
        /// <param name="createSymbols">Flag if rows in this table create symbols.</param>
        /// <param name="unreal">Flag if table is unreal.</param>
        /// <param name="bootstrapperApplicationData">Flag if table is part of UX Manifest.</param>
        public TableDefinition(string name, IList<ColumnDefinition> columns, bool createSymbols, bool unreal, bool bootstrapperApplicationData = false)
        {
            this.Name = name;
            this.CreateSymbols = createSymbols;
            this.Unreal = unreal;
            this.BootstrapperApplicationData = bootstrapperApplicationData;

            this.Columns = new ReadOnlyCollection<ColumnDefinition>(columns);
        }

        /// <summary>
        /// Gets if rows in this table create symbols.
        /// </summary>
        /// <value>Flag if rows in this table create symbols.</value>
        public bool CreateSymbols { get; private set; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets if the table is unreal.
        /// </summary>
        /// <value>Flag if table is unreal.</value>
        public bool Unreal { get; private set; }

        /// <summary>
        /// Gets if the table is a part of the bootstrapper application data manifest.
        /// </summary>
        /// <value>Flag if table is a part of the bootstrapper application data manifest.</value>
        public bool BootstrapperApplicationData { get; private set; }

        /// <summary>
        /// Gets the collection of column definitions for this table.
        /// </summary>
        /// <value>Collection of column definitions for this table.</value>
        public IList<ColumnDefinition> Columns { get; private set; }

        /// <summary>
        /// Gets the column definition in the table by index.
        /// </summary>
        /// <param name="columnIndex">Index of column to locate.</param>
        /// <value>Column definition in the table by index.</value>
        public ColumnDefinition this[int columnIndex]
        {
            get { return this.Columns[columnIndex]; }
        }

        /// <summary>
        /// Gets the table definition in IDT format.
        /// </summary>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <returns>Table definition in IDT format.</returns>
        public string ToIdtDefinition(bool keepAddedColumns)
        {
            bool first = true;
            StringBuilder columnString = new StringBuilder();
            StringBuilder dataString = new StringBuilder();
            StringBuilder tableString = new StringBuilder();

            tableString.Append(this.Name);
            foreach (ColumnDefinition column in this.Columns)
            {
                // conditionally keep columns added in a transform; otherwise,
                // break because columns can only be added at the end
                if (column.Added && !keepAddedColumns)
                {
                    break;
                }

                if (!first)
                {
                    columnString.Append('\t');
                    dataString.Append('\t');
                }

                columnString.Append(column.Name);
                dataString.Append(column.IdtType);

                if (column.PrimaryKey)
                {
                    tableString.AppendFormat("\t{0}", column.Name);
                }

                first = false;
            }
            columnString.Append("\r\n");
            columnString.Append(dataString);
            columnString.Append("\r\n");
            columnString.Append(tableString);
            columnString.Append("\r\n");

            return columnString.ToString();
        }

        /// <summary>
        /// Adds the validation rows to the _Validation table.
        /// </summary>
        /// <param name="validationTable">The _Validation table.</param>
        public void AddValidationRows(Table validationTable)
        {
            foreach (ColumnDefinition columnDef in this.Columns)
            {
                Row row = validationTable.CreateRow(null);

                row[0] = this.Name;

                row[1] = columnDef.Name;

                if (columnDef.Nullable)
                {
                    row[2] = "Y";
                }
                else
                {
                    row[2] = "N";
                }

                if (columnDef.IsMinValueSet)
                {
                    row[3] = columnDef.MinValue;
                }

                if (columnDef.IsMaxValueSet)
                {
                    row[4] = columnDef.MaxValue;
                }

                row[5] = columnDef.KeyTable;

                if (columnDef.IsKeyColumnSet)
                {
                    row[6] = columnDef.KeyColumn;
                }

                if (ColumnCategory.Unknown != columnDef.Category)
                {
                    row[7] = columnDef.Category.ToString();
                }

                row[8] = columnDef.Possibilities;

                row[9] = columnDef.Description;
            }
        }

        /// <summary>
        /// Compares this table definition to another table definition.
        /// </summary>
        /// <remarks>
        /// Only Windows Installer traits are compared, allowing for updates to WiX-specific table definitions.
        /// </remarks>
        /// <param name="updated">The updated <see cref="TableDefinition"/> to compare with this target definition.</param>
        /// <returns>0 if the tables' core properties are the same; otherwise, non-0.</returns>
        public int CompareTo(TableDefinition updated)
        {
            // by definition, this object is greater than null
            if (null == updated)
            {
                return 1;
            }

            // compare the table names
            int ret = String.Compare(this.Name, updated.Name, StringComparison.Ordinal);

            // compare the column count
            if (0 == ret)
            {
                // transforms can only add columns
                ret = Math.Min(0, updated.Columns.Count - this.Columns.Count);

                // compare name, type, and length of each column
                for (int i = 0; 0 == ret && this.Columns.Count > i; i++)
                {
                    ColumnDefinition thisColumnDef = this.Columns[i];
                    ColumnDefinition updatedColumnDef = updated.Columns[i];

                    ret = thisColumnDef.CompareTo(updatedColumnDef);
                }
            }

            return ret;
        }

        /// <summary>
        /// Parses table definition from xml reader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The TableDefintion represented by the Xml.</returns>
        internal static TableDefinition Read(XmlReader reader)
        {
            bool empty = reader.IsEmptyElement;
            bool createSymbols = false;
            string name = null;
            bool unreal = false;
            bool bootstrapperApplicationData = false;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "createSymbols":
                        createSymbols = reader.Value.Equals("yes");
                        break;
                    case "name":
                        name = reader.Value;
                        break;
                    case "unreal":
                        unreal = reader.Value.Equals("yes");
                        break;
                    case "bootstrapperApplicationData":
                        bootstrapperApplicationData = reader.Value.Equals("yes");
                        break;
                }
            }

            if (null == name)
            {
                throw new XmlException();
            }

            List<ColumnDefinition> columns = new List<ColumnDefinition>();
            bool hasPrimaryKeyColumn = false;

            // parse the child elements
            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "columnDefinition":
                                    ColumnDefinition columnDefinition = ColumnDefinition.Read(reader);
                                    columns.Add(columnDefinition);

                                    if (columnDefinition.PrimaryKey)
                                    {
                                        hasPrimaryKeyColumn = true;
                                    }
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

                if (!unreal && !bootstrapperApplicationData && !hasPrimaryKeyColumn)
                {
                    throw new WixException(WixDataErrors.RealTableMissingPrimaryKeyColumn(SourceLineNumber.CreateFromUri(reader.BaseURI), name));
                }

                if (!done)
                {
                    throw new XmlException();
                }
            }

            TableDefinition tableDefinition = new TableDefinition(name, columns, createSymbols, unreal, bootstrapperApplicationData);
            return tableDefinition;
        }

        /// <summary>
        /// Persists an output in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("tableDefinition", TableDefinitionCollection.XmlNamespaceUri);

            writer.WriteAttributeString("name", this.Name);

            if (this.CreateSymbols)
            {
                writer.WriteAttributeString("createSymbols", "yes");
            }

            if (this.Unreal)
            {
                writer.WriteAttributeString("unreal", "yes");
            }

            if (this.BootstrapperApplicationData)
            {
                writer.WriteAttributeString("bootstrapperApplicationData", "yes");
            }

            foreach (ColumnDefinition columnDefinition in this.Columns)
            {
                columnDefinition.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
