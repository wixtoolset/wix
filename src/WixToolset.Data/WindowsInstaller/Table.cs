// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Xml;
    using WixToolset.Data.WindowsInstaller.Rows;

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
    }
}
