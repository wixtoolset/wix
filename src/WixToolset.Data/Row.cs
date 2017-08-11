// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Row containing data for a table.
    /// </summary>
    public class Row
    {
        private static long rowCount;

        private Field[] fields;

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
            this.fields = new Field[tableDefinition.Columns.Count];
            this.TableDefinition = tableDefinition;

            for (int i = 0; i < this.fields.Length; ++i)
            {
                this.fields[i] = Field.Create(this.TableDefinition.Columns[i]);
            }
        }

        /// <summary>
        /// Creates a shallow copy of a row from another row.
        /// </summary>
        /// <param name="source">The row the data is copied from.</param>
        protected Row(Row source)
        {
            this.Table = source.Table;
            this.TableDefinition = source.TableDefinition;
            this.Number = source.Number;
            this.Access = source.Access;
            this.Operation = source.Operation;
            this.Redundant = source.Redundant;
            this.SectionId = source.SectionId;
            this.SourceLineNumbers = source.SourceLineNumbers;
            this.fields = source.fields;
        }

        /// <summary>
        /// Gets or sets the access to the row's primary key.
        /// </summary>
        /// <value>The row access modifier.</value>
        public AccessModifier Access { get; set; }

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
        /// Gets the section for the row.
        /// </summary>
        /// <value>Section for the row.</value>
        public Section Section { get { return (null == this.Table) ? null : this.Table.Section; } }

        /// <summary>
        /// Gets or sets the SectionId property on the row.
        /// </summary>
        /// <value>The SectionId property on the row.</value>
        public string SectionId { get; set; }

        /// <summary>
        /// Gets the source file and line number for the row.
        /// </summary>
        /// <value>Source file and line number.</value>
        public SourceLineNumber SourceLineNumbers { get; private set; }

        /// <summary>
        /// Gets the table this row belongs to.
        /// </summary>
        /// <value>null if Row does not belong to a Table, or owner Table otherwise.</value>
        public Table Table { get; private set; }

        /// <summary>
        /// Gets the table definition for this row.
        /// </summary>
        /// <remarks>A Row always has a TableDefinition, even if the Row does not belong to a Table.</remarks>
        /// <value>TableDefinition for Row.</value>
        public TableDefinition TableDefinition { get; private set; }

        /// <summary>
        /// Gets the fields contained by this row.
        /// </summary>
        /// <value>Array of field objects</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Field[] Fields
        {
            get { return this.fields; }
        }

        /// <summary>
        /// Gets the unique number for the row.
        /// </summary>
        /// <value>Number for row.</value>
        public long Number { get; private set; }

        /// <summary>
        /// Gets or sets the value of a particular field in the row.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <value>Value of a field in the row.</value>
        public object this[int field]
        {
            get { return this.fields[field].Data; }
            set { this.fields[field].Data = value; }
        }

        /// <summary>
        /// Gets the field as an integer.
        /// </summary>
        /// <returns>Field's data as an integer.</returns>
        public int FieldAsInteger(int field)
        {
            return this.fields[field].AsInteger();
        }

        /// <summary>
        /// Gets the field as an integer that could be null.
        /// </summary>
        /// <returns>Field's data as an integer that could be null.</returns>
        public int? FieldAsNullableInteger(int field)
        {
            return this.fields[field].AsNullableInteger();
        }

        /// <summary>
        /// Gets the field as a string.
        /// </summary>
        /// <returns>Field's data as a string.</returns>
        public string FieldAsString(int field)
        {
            return this.fields[field].AsString();
        }

        /// <summary>
        /// Sets the value of a particular field in the row without validating.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <param name="value">Value of a field in the row.</param>
        /// <returns>True if successful, false if validation failed.</returns>
        public bool BestEffortSetField(int field, object value)
        {
            return this.fields[field].BestEffortSet(value);
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
            bool foundPrimaryKey = false;
            StringBuilder primaryKey = new StringBuilder();

            foreach (Field field in this.fields)
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
        /// Returns true if the specified field is null or an empty string.
        /// </summary>
        /// <param name="field">Index of the field to check.</param>
        /// <returns>true if the specified field is null or an empty string, false otherwise.</returns>
        public bool IsColumnEmpty(int field)
        {
            if (null == this.fields[field].Data)
            {
                return true;
            }

            string dataString = this.fields[field].Data as string;
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
            bool identical = (this.TableDefinition.Name == row.TableDefinition.Name && this.fields.Length == row.fields.Length);

            for (int i = 0; identical && i < this.fields.Length; ++i)
            {
                if (!(this.fields[i].IsIdentical(row.fields[i])))
                {
                    identical = false;
                }
            }

            return identical;
        }

        /// <summary>
        /// Returns a string representation of the Row.
        /// </summary>
        /// <returns>A string representation of the Row.</returns>
        public override string ToString()
        {
            return String.Join("/", (object[])this.fields);
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
            AccessModifier access = AccessModifier.Public;
            RowOperation operation = RowOperation.None;
            bool redundant = false;
            string sectionId = null;
            SourceLineNumber sourceLineNumbers = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "access":
                        access = (AccessModifier)Enum.Parse(typeof(AccessModifier), reader.Value, true);
                        break;
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

            Row row = table.CreateRow(sourceLineNumbers);
            row.Access = access;
            row.Operation = operation;
            row.Redundant = redundant;
            row.SectionId = sectionId;

            // loop through all the fields in a row
            if (!empty)
            {
                bool done = false;
                int field = 0;

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
                                        row.fields[field].Read(reader);
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
        /// Returns the row in a format usable in IDT files.
        /// </summary>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <returns>String with tab delimited field values.</returns>
        internal string ToIdtDefinition(bool keepAddedColumns)
        {
            bool first = true;
            StringBuilder sb = new StringBuilder();

            foreach (Field field in this.fields)
            {
                // Conditionally keep columns added in a transform; otherwise,
                // break because columns can only be added at the end.
                if (field.Column.Added && !keepAddedColumns)
                {
                    break;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append('\t');
                }

                sb.Append(field.ToIdtValue());
            }
            sb.Append("\r\n");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the modularized version of the field data.
        /// </summary>
        /// <param name="field">The field to modularize.</param>
        /// <param name="modularizationGuid">String containing the GUID of the Merge Module to append the the field value, if appropriate.</param>
        /// <param name="suppressModularizationIdentifiers">Optional collection of identifiers that should not be modularized.</param>
        /// <remarks>moduleGuid is expected to be null when not being used to compile a Merge Module.</remarks>
        /// <returns>The modularized version of the field data.</returns>
        internal string GetModularizedValue(Field field, string modularizationGuid, ISet<string> suppressModularizationIdentifiers)
        {
            Debug.Assert(null != field.Data && 0 < ((string)field.Data).Length);
            string fieldData = Convert.ToString(field.Data, CultureInfo.InvariantCulture);

            if (null != modularizationGuid && ColumnModularizeType.None != field.Column.ModularizeType && !(WindowsInstallerStandard.IsStandardAction(fieldData) || WindowsInstallerStandard.IsStandardProperty(fieldData)))
            {
                StringBuilder sb;
                int start;
                ColumnModularizeType modularizeType = field.Column.ModularizeType;

                // special logic for the ControlEvent table's Argument column
                // this column requires different modularization methods depending upon the value of the Event column
                if (ColumnModularizeType.ControlEventArgument == field.Column.ModularizeType)
                {
                    switch (this[2].ToString())
                    {
                        case "CheckExistingTargetPath": // redirectable property name
                        case "CheckTargetPath":
                        case "DoAction": // custom action name
                        case "NewDialog": // dialog name
                        case "SelectionBrowse":
                        case "SetTargetPath":
                        case "SpawnDialog":
                        case "SpawnWaitDialog":
                            if (Common.IsIdentifier(fieldData))
                            {
                                modularizeType = ColumnModularizeType.Column;
                            }
                            else
                            {
                                modularizeType = ColumnModularizeType.Property;
                            }
                            break;
                        default: // formatted
                            modularizeType = ColumnModularizeType.Property;
                            break;
                    }
                }
                else if (ColumnModularizeType.ControlText == field.Column.ModularizeType)
                {
                    // icons are stored in the Binary table, so they get column-type modularization
                    if (("Bitmap" == this[2].ToString() || "Icon" == this[2].ToString()) && Common.IsIdentifier(fieldData))
                    {
                        modularizeType = ColumnModularizeType.Column;
                    }
                    else
                    {
                        modularizeType = ColumnModularizeType.Property;
                    }
                }

                switch (modularizeType)
                {
                    case ColumnModularizeType.Column:
                        // ensure the value is an identifier (otherwise it shouldn't be modularized this way)
                        if (!Common.IsIdentifier(fieldData))
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_CannotModularizeIllegalID, fieldData));
                        }

                        // if we're not supposed to suppress modularization of this identifier
                        if (null == suppressModularizationIdentifiers || !suppressModularizationIdentifiers.Contains(fieldData))
                        {
                            fieldData = String.Concat(fieldData, ".", modularizationGuid);
                        }
                        break;

                    case ColumnModularizeType.Property:
                    case ColumnModularizeType.Condition:
                        Regex regex;
                        if (ColumnModularizeType.Property == modularizeType)
                        {
                            regex = new Regex(@"\[(?<identifier>[#$!]?[a-zA-Z_][a-zA-Z0-9_\.]*)]", RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                        }
                        else
                        {
                            Debug.Assert(ColumnModularizeType.Condition == modularizeType);

                            // This heinous looking regular expression is actually quite an elegant way 
                            // to shred the entire condition into the identifiers that need to be 
                            // modularized.  Let's break it down piece by piece:
                            //
                            // 1. Look for the operators: NOT, EQV, XOR, OR, AND, IMP (plus a space).  Note that the
                            //    regular expression is case insensitive so we don't have to worry about
                            //    all the permutations of these strings.
                            // 2. Look for quoted strings.  Quoted strings are just text and are ignored 
                            //    outright.
                            // 3. Look for environment variables.  These look like identifiers we might 
                            //    otherwise be interested in but start with a percent sign.  Like quoted 
                            //    strings these enviroment variable references are ignored outright.
                            // 4. Match all identifiers that are things that need to be modularized.  Note
                            //    the special characters (!, $, ?, &) that denote Component and Feature states.
                            regex = new Regex(@"NOT\s|EQV\s|XOR\s|OR\s|AND\s|IMP\s|"".*?""|%[a-zA-Z_][a-zA-Z0-9_\.]*|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

                            // less performant version of the above with captures showing where everything lives
                            // regex = new Regex(@"(?<operator>NOT|EQV|XOR|OR|AND|IMP)|(?<string>"".*?"")|(?<environment>%[a-zA-Z_][a-zA-Z0-9_\.]*)|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)",RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                        }

                        MatchCollection matches = regex.Matches(fieldData);

                        sb = new StringBuilder(fieldData);

                        // notice how this code walks backward through the list
                        // because it modifies the string as we through it
                        for (int i = matches.Count - 1; 0 <= i; i--)
                        {
                            Group group = matches[i].Groups["identifier"];
                            if (group.Success)
                            {
                                string identifier = group.Value;
                                if (!WindowsInstallerStandard.IsStandardProperty(identifier) && (null == suppressModularizationIdentifiers || !suppressModularizationIdentifiers.Contains(identifier)))
                                {
                                    sb.Insert(group.Index + group.Length, '.');
                                    sb.Insert(group.Index + group.Length + 1, modularizationGuid);
                                }
                            }
                        }

                        fieldData = sb.ToString();
                        break;

                    case ColumnModularizeType.CompanionFile:
                        // if we're not supposed to ignore this identifier and the value does not start with
                        // a digit, we must have a companion file so modularize it
                        if ((null == suppressModularizationIdentifiers || !suppressModularizationIdentifiers.Contains(fieldData)) &&
                            0 < fieldData.Length && !Char.IsDigit(fieldData, 0))
                        {
                            fieldData = String.Concat(fieldData, ".", modularizationGuid);
                        }
                        break;

                    case ColumnModularizeType.Icon:
                        if (null == suppressModularizationIdentifiers || !suppressModularizationIdentifiers.Contains(fieldData))
                        {
                            start = fieldData.LastIndexOf(".", StringComparison.Ordinal);
                            if (-1 == start)
                            {
                                fieldData = String.Concat(fieldData, ".", modularizationGuid);
                            }
                            else
                            {
                                fieldData = String.Concat(fieldData.Substring(0, start), ".", modularizationGuid, fieldData.Substring(start));
                            }
                        }
                        break;

                    case ColumnModularizeType.SemicolonDelimited:
                        string[] keys = fieldData.Split(';');
                        for (int i = 0; i < keys.Length; ++i)
                        {
                            keys[i] = String.Concat(keys[i], ".", modularizationGuid);
                        }
                        fieldData = String.Join(";", keys);
                        break;
                }
            }

            return fieldData;
        }

        /// <summary>
        /// Persists a row in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way intermediate files are generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("row", Intermediate.XmlNamespaceUri);

            if (AccessModifier.Public != this.Access)
            {
                writer.WriteAttributeString("access", this.Access.ToString().ToLowerInvariant());
            }

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

            for (int i = 0; i < this.fields.Length; ++i)
            {
                this.fields[i].Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
