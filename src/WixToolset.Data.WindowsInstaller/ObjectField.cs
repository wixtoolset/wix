// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Field containing data for an object column in a row.
    /// </summary>
    public sealed class ObjectField : Field
    {
        /// <summary>
        /// Instantiates a new Field.
        /// </summary>
        /// <param name="columnDefinition">Column definition for this field.</param>
        internal ObjectField(ColumnDefinition columnDefinition) :
            base(columnDefinition)
        {
        }

        /// <summary>
        /// Gets or sets the index of the embedded file in a library.
        /// </summary>
        /// <value>The index of the embedded file.</value>
        public int? EmbeddedFileIndex { get; set; }

        /// <summary>
        /// Gets or sets the previous index of the embedded file in the library.
        /// </summary>
        /// <value>The previous index of the embedded file.</value>
        public int? PreviousEmbeddedFileIndex { get; set; }

        /// <summary>
        /// Gets or sets the path to the embedded cabinet of the previous file.
        /// </summary>
        /// <value>The path of the cabinet containing the previous file.</value>
        public Uri PreviousBaseUri { get; set; }

        /// <summary>
        /// Gets the base URI of the object field.
        /// </summary>
        /// <value>The base URI of the object field.</value>
        public Uri BaseUri { get; private set; }

        /// <summary>
        /// Gets or sets the unresolved data for this field.
        /// </summary>
        /// <value>Unresolved Data in the field.</value>
        public string UnresolvedData { get; set; }

        /// <summary>
        /// Gets or sets the unresolved previous data.
        /// </summary>
        /// <value>The unresolved previous data.</value>
        public string UnresolvedPreviousData { get; set; }

        /// <summary>
        /// Parse a field from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        internal override void Read(XmlReader reader)
        {
            Debug.Assert("field" == reader.LocalName);

            bool empty = reader.IsEmptyElement;

            this.BaseUri = new Uri(reader.BaseURI);

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "cabinetFileId":
                        this.EmbeddedFileIndex = Convert.ToInt32(reader.Value);
                        break;
                    case "modified":
                        this.Modified = reader.Value.Equals("yes");
                        break;
                    case "previousData":
                        this.PreviousData = reader.Value;
                        break;
                    case "unresolvedPreviousData":
                        this.UnresolvedPreviousData = reader.Value;
                        break;
                    case "unresolvedData":
                        this.UnresolvedData = reader.Value;
                        break;
                    case "previousCabinetFileId":
                        this.PreviousEmbeddedFileIndex = Convert.ToInt32(reader.Value);
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
                            if (0 < reader.Value.Length)
                            {
                                this.Data = reader.Value;
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
        internal override void Write(XmlWriter writer)
        {
            writer.WriteStartElement("field", Intermediate.XmlNamespaceUri);

            if (this.EmbeddedFileIndex.HasValue)
            {
                writer.WriteStartAttribute("cabinetFileId");
                writer.WriteValue(this.EmbeddedFileIndex);
                writer.WriteEndAttribute();
            }

            if (this.Modified)
            {
                writer.WriteAttributeString("modified", "yes");
            }

            if (null != this.UnresolvedPreviousData)
            {
                writer.WriteAttributeString("unresolvedPreviousData", this.UnresolvedPreviousData);
            }

            if (null != this.PreviousData)
            {
                writer.WriteAttributeString("previousData", this.PreviousData);
            }

            if (null != this.UnresolvedData)
            {
                writer.WriteAttributeString("unresolvedData", this.UnresolvedData);
            }

            if (this.PreviousEmbeddedFileIndex.HasValue)
            {
                writer.WriteStartAttribute("previousCabinetFileId");
                writer.WriteValue(this.PreviousEmbeddedFileIndex);
                writer.WriteEndAttribute();
            }

            // Convert the data to a string that will persist nicely (nulls as String.Empty).
            string text = Convert.ToString(this.Data, CultureInfo.InvariantCulture);
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
