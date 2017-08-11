// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Field containing data for a column in a row.
    /// </summary>
    public class Field
    {
        private object data;

        /// <summary>
        /// Instantiates a new Field.
        /// </summary>
        /// <param name="columnDefinition">Column definition for this field.</param>
        protected Field(ColumnDefinition columnDefinition)
        {
            this.Column = columnDefinition;
        }

        /// <summary>
        /// Gets or sets the column definition for this field.
        /// </summary>
        /// <value>Column definition.</value>
        public ColumnDefinition Column { get; private set; }

        /// <summary>
        /// Gets or sets the data for this field.
        /// </summary>
        /// <value>Data in the field.</value>
        public object Data
        {
            get
            {
                return this.data;
            }

            set
            {
                // Validate the value before setting it.
                this.data = this.Column.ValidateValue(value);
            }
        }

        /// <summary>
        /// Gets or sets whether this field is modified.
        /// </summary>
        /// <value>Whether this field is modified.</value>
        public bool Modified { get; set; }

        /// <summary>
        /// Gets or sets the previous data.
        /// </summary>
        /// <value>The previous data.</value>
        public string PreviousData { get; set; }

        /// <summary>
        /// Instantiate a new Field object of the correct type.
        /// </summary>
        /// <param name="columnDefinition">The column definition for the field.</param>
        /// <returns>The new Field object.</returns>
        public static Field Create(ColumnDefinition columnDefinition)
        {
            return (ColumnType.Object == columnDefinition.Type) ? new ObjectField(columnDefinition) : new Field(columnDefinition);
        }

        /// <summary>
        /// Sets the value of a particular field in the row without validating.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <param name="value">Value of a field in the row.</param>
        /// <returns>True if successful, false if validation failed.</returns>
        public bool BestEffortSet(object value)
        {
            bool success = true;
            object bestEffortValue = value;

            try
            {
                bestEffortValue = this.Column.ValidateValue(value);
            }
            catch (InvalidOperationException)
            {
                success = false;
            }

            this.data = bestEffortValue;
            return success;
        }

        /// <summary>
        /// Determine if this field is identical to another field.
        /// </summary>
        /// <param name="field">The other field to compare to.</param>
        /// <returns>true if they are equal; false otherwise.</returns>
        public bool IsIdentical(Field field)
        {
            return (this.Column.Name == field.Column.Name &&
                ((null != this.data && this.data.Equals(field.data)) || (null == this.data && null == field.data)));
        }

        /// <summary>
        /// Overrides the built in object implementation to return the field's data as a string.
        /// </summary>
        /// <returns>Field's data as a string.</returns>
        public override string ToString()
        {
            return this.AsString();
        }

        /// <summary>
        /// Gets the field as an integer.
        /// </summary>
        /// <returns>Field's data as an integer.</returns>
        public int AsInteger()
        {
            return (this.data is int) ? (int)this.data : Convert.ToInt32(this.data, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the field as an integer that could be null.
        /// </summary>
        /// <returns>Field's data as an integer that could be null.</returns>
        public int? AsNullableInteger()
        {
            return (null == this.data) ? (int?)null : (this.data is int) ? (int)this.data : Convert.ToInt32(this.data, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the field as a string.
        /// </summary>
        /// <returns>Field's data as a string.</returns>
        public string AsString()
        {
            return (null == this.data) ? null : Convert.ToString(this.data, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse a field from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        internal virtual void Read(XmlReader reader)
        {
            Debug.Assert("field" == reader.LocalName);

            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "modified":
                        this.Modified = reader.Value.Equals("yes");
                        break;
                    case "previousData":
                        this.PreviousData = reader.Value;
                        break;
                }
            }

            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            throw new XmlException();
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                            if (0 < reader.Value.Length)
                            {
                                if (ColumnType.Number == this.Column.Type && !this.Column.IsLocalizable)
                                {
                                    // older wix files could persist data as a long value (which would overflow an int)
                                    // since the Convert class always throws exceptions for overflows, read in integral
                                    // values as a long to avoid the overflow, then cast it to an int (this operation can
                                    // overflow without throwing an exception inside an unchecked block)
                                    this.data = unchecked((int)Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    this.data = reader.Value;
                                }
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
        }

        /// <summary>
        /// Persists a field in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Field should persist itself as XML.</param>
        internal virtual void Write(XmlWriter writer)
        {
            writer.WriteStartElement("field", Intermediate.XmlNamespaceUri);

            if (this.Modified)
            {
                writer.WriteAttributeString("modified", "yes");
            }

            if (null != this.PreviousData)
            {
                writer.WriteAttributeString("previousData", this.PreviousData);
            }

            // Convert the data to a string that will persist nicely (nulls as String.Empty).
            string text = Convert.ToString(this.data, CultureInfo.InvariantCulture);
            if (this.Column.UseCData)
            {
                writer.WriteCData(text);
            }
            else
            {
                writer.WriteString(text);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Returns the field data in a format usable in IDT files.
        /// </summary>
        /// <returns>Field data in string IDT format.</returns>
        internal string ToIdtValue()
        {
            if (null == this.data)
            {
                return null;
            }
            else
            {
                string fieldData = Convert.ToString(this.data, CultureInfo.InvariantCulture);

                // special idt-specific escaping
                if (this.Column.EscapeIdtCharacters)
                {
                    fieldData = fieldData.Replace('\t', '\x10');
                    fieldData = fieldData.Replace('\r', '\x11');
                    fieldData = fieldData.Replace('\n', '\x19');
                }

                return fieldData;
            }
        }
    }
}
