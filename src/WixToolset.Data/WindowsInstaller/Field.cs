// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
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
            get => this.data;
            set => this.data = this.ValidateValue(this.Column, value);
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
                bestEffortValue = this.ValidateValue(this.Column, value);
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
        /// Validate a value for this column.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>Validated value.</returns>
        internal object ValidateValue(ColumnDefinition column, object value)
        {
            if (null == value)
            {
                if (!column.Nullable)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set column '{0}' with a null value because this is a required field.", column.Name));
                }
            }
            else // check numerical values against their specified minimum and maximum values.
            {
                if (ColumnType.Number == column.Type && !column.IsLocalizable)
                {
                    // For now all enums in the tables can be represented by integers. This if statement would need to
                    // be enhanced if that ever changes.
                    if (value is int || value.GetType().IsEnum)
                    {
                        var intValue = (int)value;

                        // validate the value against the minimum allowed value
                        if (column.MinValue.HasValue && column.MinValue > intValue)
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set column '{0}' with value {1} because it is less than the minimum allowed value for this column, {2}.", column.Name, intValue, column.MinValue));
                        }

                        // validate the value against the maximum allowed value
                        if (column.MaxValue.HasValue && column.MaxValue < intValue)
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set column '{0}' with value {1} because it is greater than the maximum allowed value for this column, {2}.", column.Name, intValue, column.MaxValue));
                        }

                        return intValue;
                    }
                    else if (value is long longValue)
                    {
                        // validate the value against the minimum allowed value
                        if (column.MinValue.HasValue && column.MinValue > longValue)
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set column '{0}' with value {1} because it is less than the minimum allowed value for this column, {2}.", column.Name, longValue, column.MinValue));
                        }

                        // validate the value against the maximum allowed value
                        if (column.MaxValue.HasValue && column.MaxValue < longValue)
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set column '{0}' with value {1} because it is greater than the maximum allowed value for this column, {2}.", column.Name, longValue, column.MaxValue));
                        }

                        return longValue;
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set number column '{0}' with a value of type '{1}'.", column.Name, value.GetType().ToString()));
                    }
                }
                else
                {
                    if (!(value is string))
                    {
                        //throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot set string column '{0}' with a value of type '{1}'.", this.name, value.GetType().ToString()));
                        return value.ToString();
                    }
                }
            }

            return value;
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
            writer.WriteStartElement("field", WindowsInstallerData.XmlNamespaceUri);

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
    }
}
