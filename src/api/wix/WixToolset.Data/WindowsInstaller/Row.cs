// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Row containing data for a table.
    /// </summary>
    public class Row
    {
        private static long rowCount;

        /// <summary>
        /// Creates a row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        /// <remarks>The compiler should use this constructor exclusively.</remarks>
        public Row(SourceLineNumber sourceLineNumbers, Table table)
            : this(sourceLineNumbers, table.Definition)
        {
            this.Table = table;
        }

        /// <summary>
        /// Creates a row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDefinition">TableDefinition this row should get its column definitions from.</param>
        /// <remarks>This constructor is used in cases where there isn't a clear owner of the row.  The linker uses this constructor for the rows it generates.</remarks>
        public Row(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
        {
            this.Number = rowCount++;
            this.SourceLineNumbers = sourceLineNumbers;
            this.Fields = new Field[tableDefinition.Columns.Length];
            this.TableDefinition = tableDefinition;

            for (var i = 0; i < this.Fields.Length; ++i)
            {
                this.Fields[i] = Field.Create(this.TableDefinition.Columns[i]);
            }
        }

        /// <summary>
        /// Gets or sets the row transform operation.
        /// </summary>
        /// <value>The row transform operation.</value>
        public RowOperation Operation { get; set; }

        /// <summary>
        /// Gets or sets wether the row is a duplicate of another row thus redundant.
        /// </summary>
        public bool Redundant { get; set; }

        /// <summary>
        /// Gets or sets the SectionId property on the row.
        /// </summary>
        /// <value>The SectionId property on the row.</value>
        public string SectionId { get; set; }

        /// <summary>
        /// Gets the source file and line number for the row.
        /// </summary>
        /// <value>Source file and line number.</value>
        public SourceLineNumber SourceLineNumbers { get; }

        /// <summary>
        /// Gets the table this row belongs to.
        /// </summary>
        /// <value>null if Row does not belong to a Table, or owner Table otherwise.</value>
        public Table Table { get; }

        /// <summary>
        /// Gets the table definition for this row.
        /// </summary>
        /// <remarks>A Row always has a TableDefinition, even if the Row does not belong to a Table.</remarks>
        /// <value>TableDefinition for Row.</value>
        public TableDefinition TableDefinition { get; }

        /// <summary>
        /// Gets the fields contained by this row.
        /// </summary>
        /// <value>Array of field objects</value>
        public Field[] Fields { get; }

        /// <summary>
        /// Gets the unique number for the row.
        /// </summary>
        /// <value>Number for row.</value>
        public long Number { get; }

        /// <summary>
        /// Gets or sets the value of a particular field in the row.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <value>Value of a field in the row.</value>
        public object this[int field]
        {
            get { return this.Fields[field].Data; }
            set { this.Fields[field].Data = value; }
        }

        /// <summary>
        /// Gets the field as an integer.
        /// </summary>
        /// <returns>Field's data as an integer.</returns>
        public int FieldAsInteger(int field)
        {
            return this.Fields[field].AsInteger();
        }

        /// <summary>
        /// Gets the field as an integer that could be null.
        /// </summary>
        /// <returns>Field's data as an integer that could be null.</returns>
        public int? FieldAsNullableInteger(int field)
        {
            return this.Fields[field].AsNullableInteger();
        }

        /// <summary>
        /// Gets the field as a string.
        /// </summary>
        /// <returns>Field's data as a string.</returns>
        public string FieldAsString(int field)
        {
            return this.Fields[field].AsString();
        }

        /// <summary>
        /// Sets the value of a particular field in the row without validating.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <param name="value">Value of a field in the row.</param>
        /// <returns>True if successful, false if validation failed.</returns>
        public bool BestEffortSetField(int field, object value)
        {
            return this.Fields[field].BestEffortSet(value);
        }

        /// <summary>
        /// Get the value used to represent the row in a keyed row collection.
        /// </summary>
        /// <returns>Primary key or row number if no primary key is available.</returns>
        public string GetKey()
        {
            return this.GetPrimaryKey() ?? Convert.ToString(this.Number, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get the primary key of this row.
        /// </summary>
        /// <param name="delimiter">Delimiter character for multiple column primary keys.</param>
        /// <returns>The primary key or null if the row's table has no primary key columns.</returns>
        public string GetPrimaryKey(char delimiter = '/')
        {
            return this.GetPrimaryKey(delimiter, String.Empty);
        }

        /// <summary>
        /// Get the primary key of this row.
        /// </summary>
        /// <param name="delimiter">Delimiter character for multiple column primary keys.</param>
        /// <param name="nullReplacement">String to represent null values in the primary key.</param>
        /// <returns>The primary key or null if the row's table has no primary key columns.</returns>
        public string GetPrimaryKey(char delimiter, string nullReplacement)
        {
            var foundPrimaryKey = false;
            var primaryKey = new StringBuilder();

            foreach (var field in this.Fields)
            {
                if (field.Column.PrimaryKey)
                {
                    if (foundPrimaryKey)
                    {
                        primaryKey.Append(delimiter);
                    }

                    primaryKey.Append((null == field.Data) ? nullReplacement : Convert.ToString(field.Data, CultureInfo.InvariantCulture));

                    foundPrimaryKey = true;
                }
                else // primary keys must be the first columns of a row so the first non-primary key means we can stop looking.
                {
                    break;
                }
            }

            return foundPrimaryKey ? primaryKey.ToString() : null;
        }

        /// <summary>
        /// Returns true if the specified field is null.
        /// </summary>
        /// <param name="field">Index of the field to check.</param>
        /// <returns>true if the specified field is null, false otherwise.</returns>
        public bool IsColumnNull(int field) => this.Fields[field].Data == null;

        /// <summary>
        /// Returns true if the specified field is null or an empty string.
        /// </summary>
        /// <param name="field">Index of the field to check.</param>
        /// <returns>true if the specified field is null or an empty string, false otherwise.</returns>
        public bool IsColumnEmpty(int field)
        {
            if (this.IsColumnNull(field))
            {
                return true;
            }

            string dataString = this.Fields[field].Data as string;
            if (null != dataString && 0 == dataString.Length)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests if the passed in row is identical.
        /// </summary>
        /// <param name="row">Row to compare against.</param>
        /// <returns>True if two rows are identical.</returns>
        public bool IsIdentical(Row row)
        {
            bool identical = (this.TableDefinition.Name == row.TableDefinition.Name && this.Fields.Length == row.Fields.Length);

            for (var i = 0; identical && i < this.Fields.Length; ++i)
            {
                if (!(this.Fields[i].IsIdentical(row.Fields[i])))
                {
                    identical = false;
                }
            }

            return identical;
        }

        /// <summary>
        /// Copies this row to the target row.
        /// </summary>
        /// <param name="target">Row to copy data to.</param>
        public void CopyTo(Row target)
        {
            for (var i = 0; i < this.Fields.Length; i++)
            {
                target[i] = this[i];
            }
        }

        /// <summary>
        /// Returns a string representation of the Row.
        /// </summary>
        /// <returns>A string representation of the Row.</returns>
        public override string ToString()
        {
            return String.Join("/", (object[])this.Fields);
        }

        /// <summary>
        /// Creates a Row from the XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <param name="table">Table for this row.</param>
        /// <returns>New row object.</returns>
        internal static Row Read(XmlReader reader, Table table)
        {
            Debug.Assert("row" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            RowOperation operation = RowOperation.None;
            bool redundant = false;
            string sectionId = null;
            SourceLineNumber sourceLineNumbers = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "op":
                        operation = (RowOperation)Enum.Parse(typeof(RowOperation), reader.Value, true);
                        break;
                    case "redundant":
                        redundant = reader.Value.Equals("yes");
                        break;
                    case "sectionId":
                        sectionId = reader.Value;
                        break;
                    case "sourceLineNumber":
                        sourceLineNumbers = SourceLineNumber.CreateFromEncoded(reader.Value);
                        break;
                }
            }

            var row = table.CreateRow(sourceLineNumbers);
            row.Operation = operation;
            row.Redundant = redundant;
            row.SectionId = sectionId;

            // loop through all the fields in a row
            if (!empty)
            {
                var done = false;
                var field = 0;

                // loop through all the fields in a row
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "field":
                                    if (row.Fields.Length <= field)
                                    {
                                        if (!reader.IsEmptyElement)
                                        {
                                            throw new XmlException();
                                        }
                                    }
                                    else
                                    {
                                        row.Fields[field].Read(reader);
                                    }
                                    ++field;
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

            return row;
        }

        /// <summary>
        /// Persists a row in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("row", WindowsInstallerData.XmlNamespaceUri);

            if (RowOperation.None != this.Operation)
            {
                writer.WriteAttributeString("op", this.Operation.ToString().ToLowerInvariant());
            }

            if (this.Redundant)
            {
                writer.WriteAttributeString("redundant", "yes");
            }

            if (null != this.SectionId)
            {
                writer.WriteAttributeString("sectionId", this.SectionId);
            }

            if (null != this.SourceLineNumbers)
            {
                writer.WriteAttributeString("sourceLineNumber", this.SourceLineNumbers.GetEncoded());
            }

            for (int i = 0; i < this.Fields.Length; ++i)
            {
                this.Fields[i].Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
