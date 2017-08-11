// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Section in an object file.
    /// </summary>
    public sealed class Section
    {
        /// <summary>
        /// Creates a new section as part of an intermediate.
        /// </summary>
        /// <param name="id">Identifier for section.</param>
        /// <param name="type">Type of section.</param>
        /// <param name="codepage">Codepage for resulting database.</param>
        public Section(string id, SectionType type, int codepage)
        {
            this.Id = id;
            this.Type = type;
            this.Codepage = codepage;

            this.Tables = new TableIndexedCollection();
        }

        /// <summary>
        /// Gets the identifier for the section.
        /// </summary>
        /// <value>Section identifier.</value>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the type of the section.
        /// </summary>
        /// <value>Type of section.</value>
        public SectionType Type { get; private set; }

        /// <summary>
        /// Gets the codepage for the section.
        /// </summary>
        /// <value>Codepage for the section.</value>
        public int Codepage { get; private set; }

        /// <summary>
        /// Gets the tables in the section.
        /// </summary>
        /// <value>Tables in section.</value>
        public TableIndexedCollection Tables { get; private set; }

        /// <summary>
        /// Gets the source line information of the file containing this section.
        /// </summary>
        /// <value>The source line information of the file containing this section.</value>
        public SourceLineNumber SourceLineNumbers { get; private set; }

        /// <summary>
        /// Gets the identity of the intermediate the section is contained within.
        /// </summary>
        public string IntermediateId { get; internal set; }

        /// <summary>
        /// Gets the identity of the library when the section is contained within one.
        /// </summary>
        public string LibraryId { get; internal set; }

        /// <summary>
        /// Ensures a table is added to the section's table collection.
        /// </summary>
        /// <param name="tableDefinition">Table definition for the table.</param>
        /// <returns>Table in the section.</returns>
        public Table EnsureTable(TableDefinition tableDefinition)
        {
            Table table;
            if (!this.Tables.TryGetTable(tableDefinition.Name, out table))
            {
                table = new Table(this, tableDefinition);
                this.Tables.Add(table);
            }

            return table;
        }

        /// <summary>
        /// Parse a section from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <returns>The parsed Section.</returns>
        internal static Section Read(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            Debug.Assert("section" == reader.LocalName);

            int codepage = 0;
            bool empty = reader.IsEmptyElement;
            string id = null;
            SectionType type = SectionType.Unknown;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "codepage":
                        codepage = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "id":
                        id = reader.Value;
                        break;
                    case "type":
                        switch (reader.Value)
                        {
                            case "bundle":
                                type = SectionType.Bundle;
                                break;
                            case "fragment":
                                type = SectionType.Fragment;
                                break;
                            case "module":
                                type = SectionType.Module;
                                break;
                            case "patchCreation":
                                type = SectionType.PatchCreation;
                                break;
                            case "product":
                                type = SectionType.Product;
                                break;
                            case "patch":
                                type = SectionType.Patch;
                                break;
                            default:
                                throw new XmlException();
                        }
                        break;
                }
            }

            if (null == id && (SectionType.Unknown != type && SectionType.Fragment != type))
            {
                throw new XmlException();
            }

            if (SectionType.Unknown == type)
            {
                throw new XmlException();
            }

            Section section = new Section(id, type, codepage);
            section.SourceLineNumbers = SourceLineNumber.CreateFromUri(reader.BaseURI);

            List<Table> tables = new List<Table>();
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
                                case "table":
                                    tables.Add(Table.Read(reader, section, tableDefinitions));
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

            section.Tables = new TableIndexedCollection(tables);

            return section;
        }

        /// <summary>
        /// Persist the Section to an XmlWriter.
        /// </summary>
        /// <param name="writer">XmlWriter which reference will be persisted to.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("section", Intermediate.XmlNamespaceUri);

            if (null != this.Id)
            {
                writer.WriteAttributeString("id", this.Id);
            }

            switch (this.Type)
            {
                case SectionType.Bundle:
                    writer.WriteAttributeString("type", "bundle");
                    break;
                case SectionType.Fragment:
                    writer.WriteAttributeString("type", "fragment");
                    break;
                case SectionType.Module:
                    writer.WriteAttributeString("type", "module");
                    break;
                case SectionType.Product:
                    writer.WriteAttributeString("type", "product");
                    break;
                case SectionType.PatchCreation:
                    writer.WriteAttributeString("type", "patchCreation");
                    break;
                case SectionType.Patch:
                    writer.WriteAttributeString("type", "patch");
                    break;
            }

            if (0 != this.Codepage)
            {
                writer.WriteAttributeString("codepage", this.Codepage.ToString());
            }

            // save the rows in table order
            foreach (Table table in this.Tables.OrderBy(t => t.Name))
            {
                table.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
