// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Object that represents a table in a database.
    /// </summary>
    public sealed class Table
    {
        /// <summary>
        /// Creates a table in a section.
        /// </summary>
        /// <param name="section">Section to add table to.</param>
        /// <param name="tableDefinition">Definition of the table.</param>
        public Table(Section section, TableDefinition tableDefinition)
        {
            this.Section = section;
            this.Definition = tableDefinition;
            this.Rows = new List<Row>();
        }

        /// <summary>
        /// Gets the section for the table.
        /// </summary>
        /// <value>Section for the table.</value>
        public Section Section { get; private set; }

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        /// <value>Definition of the table.</value>
        public TableDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        public string Name
        {
            get { return this.Definition.Name; }
        }

        /// <summary>
        /// Gets or sets the table transform operation.
        /// </summary>
        /// <value>The table transform operation.</value>
        public TableOperation Operation { get; set; }

        /// <summary>
        /// Gets the rows contained in the table.
        /// </summary>
        /// <value>Rows contained in the table.</value>
        public IList<Row> Rows { get; private set; }

        /// <summary>
        /// Creates a new row in the table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="add">Specifies whether to only create the row or add it to the table automatically.</param>
        /// <returns>Row created in table.</returns>
        public Row CreateRow(SourceLineNumber sourceLineNumbers, bool add = true)
        {
            Row row;

            switch (this.Name)
            {
                case "BBControl":
                    row = new BBControlRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePackage":
                    row = new WixBundlePackageRow(sourceLineNumbers, this);
                    break;
                case "WixBundleExePackage":
                    row = new WixBundleExePackageRow(sourceLineNumbers, this);
                    break;
                case "WixBundleMsiPackage":
                    row = new WixBundleMsiPackageRow(sourceLineNumbers, this);
                    break;
                case "WixBundleMspPackage":
                    row = new WixBundleMspPackageRow(sourceLineNumbers, this);
                    break;
                case "WixBundleMsuPackage":
                    row = new WixBundleMsuPackageRow(sourceLineNumbers, this);
                    break;
                case "Component":
                    row = new ComponentRow(sourceLineNumbers, this);
                    break;
                case "WixBundleContainer":
                    row = new WixBundleContainerRow(sourceLineNumbers, this);
                    break;
                case "Control":
                    row = new ControlRow(sourceLineNumbers, this);
                    break;
                case "File":
                    row = new FileRow(sourceLineNumbers, this);
                    break;
                case "WixBundleMsiFeature":
                    row = new WixBundleMsiFeatureRow(sourceLineNumbers, this);
                    break;
                case "WixBundleMsiProperty":
                    row = new WixBundleMsiPropertyRow(sourceLineNumbers, this);
                    break;
                case "Media":
                    row = new MediaRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePayload":
                    row = new WixBundlePayloadRow(sourceLineNumbers, this);
                    break;
                case "Property":
                    row = new PropertyRow(sourceLineNumbers, this);
                    break;
                case "WixRelatedBundle":
                    row = new WixRelatedBundleRow(sourceLineNumbers, this);
                    break;
                case "WixBundleRelatedPackage":
                    row = new WixBundleRelatedPackageRow(sourceLineNumbers, this);
                    break;
                case "WixBundleRollbackBoundary":
                    row = new WixBundleRollbackBoundaryRow(sourceLineNumbers, this);
                    break;
                case "Upgrade":
                    row = new UpgradeRow(sourceLineNumbers, this);
                    break;
                case "WixBundleVariable":
                    row = new WixBundleVariableRow(sourceLineNumbers, this);
                    break;
                case "WixAction":
                    row = new WixActionRow(sourceLineNumbers, this);
                    break;
                case "WixApprovedExeForElevation":
                    row = new WixApprovedExeForElevationRow(sourceLineNumbers, this);
                    break;
                case "WixBundle":
                    row = new WixBundleRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePackageExitCode":
                    row = new WixBundlePackageExitCodeRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePatchTargetCode":
                    row = new WixBundlePatchTargetCodeRow(sourceLineNumbers, this);
                    break;
                case "WixBundleSlipstreamMsp":
                    row = new WixBundleSlipstreamMspRow(sourceLineNumbers, this);
                    break;
                case "WixBundleUpdate":
                    row = new WixBundleUpdateRow(sourceLineNumbers, this);
                    break;
                case "WixBundleCatalog":
                    row = new WixBundleCatalogRow(sourceLineNumbers, this);
                    break;
                case "WixChain":
                    row = new WixChainRow(sourceLineNumbers, this);
                    break;
                case "WixChainItem":
                    row = new WixChainItemRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePackageCommandLine":
                    row = new WixBundlePackageCommandLineRow(sourceLineNumbers, this);
                    break;
                case "WixComplexReference":
                    row = new WixComplexReferenceRow(sourceLineNumbers, this);
                    break;
                case "WixDeltaPatchFile":
                    row = new WixDeltaPatchFileRow(sourceLineNumbers, this);
                    break;
                case "WixDeltaPatchSymbolPaths":
                    row = new WixDeltaPatchSymbolPathsRow(sourceLineNumbers, this);
                    break;
                case "WixFile":
                    row = new WixFileRow(sourceLineNumbers, this);
                    break;
                case "WixGroup":
                    row = new WixGroupRow(sourceLineNumbers, this);
                    break;
                case "WixMedia":
                    row = new WixMediaRow(sourceLineNumbers, this);
                    break;
                case "WixMediaTemplate":
                    row = new WixMediaTemplateRow(sourceLineNumbers, this);
                    break;
                case "WixMerge":
                    row = new WixMergeRow(sourceLineNumbers, this);
                    break;
                case "WixPayloadProperties":
                    row = new WixPayloadPropertiesRow(sourceLineNumbers, this);
                    break;
                case "WixProperty":
                    row = new WixPropertyRow(sourceLineNumbers, this);
                    break;
                case "WixSimpleReference":
                    row = new WixSimpleReferenceRow(sourceLineNumbers, this);
                    break;
                case "WixUpdateRegistration":
                    row = new WixUpdateRegistrationRow(sourceLineNumbers, this);
                    break;
                case "WixVariable":
                    row = new WixVariableRow(sourceLineNumbers, this);
                    break;

                default:
                    row = new Row(sourceLineNumbers, this);
                    break;
            }

            if (add)
            {
                this.Rows.Add(row);
            }

            return row;
        }

        /// <summary>
        /// Parse a table from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="section">Section to populate with persisted data.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <returns>The parsed table.</returns>
        internal static Table Read(XmlReader reader, Section section, TableDefinitionCollection tableDefinitions)
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
            Table table = new Table(section, tableDefinition);
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
        /// Modularize the table.
        /// </summary>
        /// <param name="modularizationGuid">String containing the GUID of the Merge Module, if appropriate.</param>
        /// <param name="suppressModularizationIdentifiers">Optional collection of identifiers that should not be modularized.</param>
        public void Modularize(string modularizationGuid, ISet<string> suppressModularizationIdentifiers)
        {
            List<int> modularizedColumns = new List<int>();

            // find the modularized columns
            for (int i = 0; i < this.Definition.Columns.Count; i++)
            {
                if (ColumnModularizeType.None != this.Definition.Columns[i].ModularizeType)
                {
                    modularizedColumns.Add(i);
                }
            }

            if (0 < modularizedColumns.Count)
            {
                foreach (Row row in this.Rows)
                {
                    foreach (int modularizedColumn in modularizedColumns)
                    {
                        Field field = row.Fields[modularizedColumn];

                        if (null != field.Data)
                        {
                            field.Data = row.GetModularizedValue(field, modularizationGuid, suppressModularizationIdentifiers);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Persists a row in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the intermediate files are generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        internal void Write(XmlWriter writer)
        {
            if (null == writer)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("table", Intermediate.XmlNamespaceUri);
            writer.WriteAttributeString("name", this.Name);

            if (TableOperation.None != this.Operation)
            {
                writer.WriteAttributeString("op", this.Operation.ToString().ToLowerInvariant());
            }

            foreach (Row row in this.Rows)
            {
                row.Write(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the table in IDT format to the provided stream.
        /// </summary>
        /// <param name="writer">Stream to write the table to.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        public void ToIdtDefinition(StreamWriter writer, bool keepAddedColumns)
        {
            if (this.Definition.Unreal)
            {
                return;
            }

            if (TableDefinition.MaxColumnsInRealTable < this.Definition.Columns.Count)
            {
                throw new WixException(WixDataErrors.TooManyColumnsInRealTable(this.Definition.Name, this.Definition.Columns.Count, TableDefinition.MaxColumnsInRealTable));
            }

            // Tack on the table header, and flush before we start writing bytes directly to the stream.
            writer.Write(this.Definition.ToIdtDefinition(keepAddedColumns));
            writer.Flush();

            using (NonClosingStreamWrapper wrapper = new NonClosingStreamWrapper(writer.BaseStream))
            using (BufferedStream buffStream = new BufferedStream(wrapper))
            {
                // Create an encoding that replaces characters with question marks, and doesn't throw. We'll 
                // use this in case of errors
                Encoding convertEncoding = Encoding.GetEncoding(writer.Encoding.CodePage);

                foreach (Row row in this.Rows)
                {
                    if (row.Redundant)
                    {
                        continue;
                    }

                    string rowString = row.ToIdtDefinition(keepAddedColumns);
                    byte[] rowBytes;

                    try
                    {
                        // GetBytes will throw an exception if any character doesn't match our current encoding
                        rowBytes = writer.Encoding.GetBytes(rowString);
                    }
                    catch (EncoderFallbackException)
                    {
                        Messaging.Instance.OnMessage(WixDataErrors.InvalidStringForCodepage(row.SourceLineNumbers, Convert.ToString(writer.Encoding.WindowsCodePage, CultureInfo.InvariantCulture)));

                        rowBytes = convertEncoding.GetBytes(rowString);
                    }

                    buffStream.Write(rowBytes, 0, rowBytes.Length);
                }
            }
        }

        /// <summary>
        /// Validates the rows of this OutputTable and throws if it collides on
        /// primary keys.
        /// </summary>
        public void ValidateRows()
        {
            Dictionary<string, SourceLineNumber> primaryKeys = new Dictionary<string, SourceLineNumber>();

            foreach (Row row in this.Rows)
            {
                string primaryKey = row.GetPrimaryKey();

                SourceLineNumber collisionSourceLineNumber;
                if (primaryKeys.TryGetValue(primaryKey, out collisionSourceLineNumber))
                {
                    throw new WixException(WixDataErrors.DuplicatePrimaryKey(collisionSourceLineNumber, primaryKey, this.Definition.Name));
                }

                primaryKeys.Add(primaryKey, row.SourceLineNumbers);
            }
        }
    }
}
