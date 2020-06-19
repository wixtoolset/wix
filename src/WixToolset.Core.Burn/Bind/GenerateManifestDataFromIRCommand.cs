// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Burn.ExtensibilityServices;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class GenerateManifestDataFromIRCommand
    {
        public GenerateManifestDataFromIRCommand(IMessaging messaging, IntermediateSection section, IEnumerable<IBurnBackendExtension> backendExtensions, IBurnBackendHelper backendHelper, IDictionary<string, IList<IntermediateTuple>> extensionSearchTuplesById)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.BackendExtensions = backendExtensions;
            this.BackendHelper = backendHelper;
            this.ExtensionSearchTuplesById = extensionSearchTuplesById;
        }

        private IEnumerable<IBurnBackendExtension> BackendExtensions { get; }

        private IBurnBackendHelper BackendHelper { get; }

        private IDictionary<string, IList<IntermediateTuple>> ExtensionSearchTuplesById { get; }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var tuples = this.Section.Tuples.ToList();
            var cellsByTableAndRowId = new Dictionary<string, List<WixCustomTableCellTuple>>();
            var customTablesById = new Dictionary<string, WixCustomTableTuple>();

            foreach (var kvp in this.ExtensionSearchTuplesById)
            {
                var extensionId = kvp.Key;
                var extensionSearchTuples = kvp.Value;
                foreach (var extensionSearchTuple in extensionSearchTuples)
                {
                    this.BackendHelper.AddBundleExtensionData(extensionId, extensionSearchTuple, tupleIdIsIdAttribute: true);
                    tuples.Remove(extensionSearchTuple);
                }
            }

            foreach (var tuple in tuples)
            {
                var unknownTuple = false;
                switch (tuple.Definition.Type)
                {
                    // Tuples used internally and are not added to a data manifest.
                    case TupleDefinitionType.ProvidesDependency:
                    case TupleDefinitionType.WixApprovedExeForElevation:
                    case TupleDefinitionType.WixBootstrapperApplication:
                    case TupleDefinitionType.WixBundle:
                    case TupleDefinitionType.WixBundleCatalog:
                    case TupleDefinitionType.WixBundleContainer:
                    case TupleDefinitionType.WixBundleExePackage:
                    case TupleDefinitionType.WixBundleExtension:
                    case TupleDefinitionType.WixBundleMsiFeature:
                    case TupleDefinitionType.WixBundleMsiPackage:
                    case TupleDefinitionType.WixBundleMsiProperty:
                    case TupleDefinitionType.WixBundleMspPackage:
                    case TupleDefinitionType.WixBundleMsuPackage:
                    case TupleDefinitionType.WixBundlePackage:
                    case TupleDefinitionType.WixBundlePackageCommandLine:
                    case TupleDefinitionType.WixBundlePackageExitCode:
                    case TupleDefinitionType.WixBundlePatchTargetCode:
                    case TupleDefinitionType.WixBundlePayload:
                    case TupleDefinitionType.WixBundleRelatedPackage:
                    case TupleDefinitionType.WixBundleRollbackBoundary:
                    case TupleDefinitionType.WixBundleSlipstreamMsp:
                    case TupleDefinitionType.WixBundleUpdate:
                    case TupleDefinitionType.WixBundleVariable:
                    case TupleDefinitionType.WixChain:
                    case TupleDefinitionType.WixComponentSearch:
                    case TupleDefinitionType.WixCustomTableColumn:
                    case TupleDefinitionType.WixDependencyProvider:
                    case TupleDefinitionType.WixFileSearch:
                    case TupleDefinitionType.WixGroup:
                    case TupleDefinitionType.WixProductSearch:
                    case TupleDefinitionType.WixRegistrySearch:
                    case TupleDefinitionType.WixRelatedBundle:
                    case TupleDefinitionType.WixSearch:
                    case TupleDefinitionType.WixSearchRelation:
                    case TupleDefinitionType.WixSetVariable:
                    case TupleDefinitionType.WixUpdateRegistration:
                        break;

                    case TupleDefinitionType.WixCustomTable:
                        unknownTuple = !this.IndexCustomTableTuple((WixCustomTableTuple)tuple, customTablesById);
                        break;

                    case TupleDefinitionType.WixCustomTableCell:
                        this.IndexCustomTableCellTuple((WixCustomTableCellTuple)tuple, cellsByTableAndRowId);
                        break;

                    case TupleDefinitionType.MustBeFromAnExtension:
                        unknownTuple = !this.AddTupleFromExtension(tuple);
                        break;

                    default:
                        unknownTuple = true;
                        break;
                }

                if (unknownTuple)
                {
                    this.Messaging.Write(WarningMessages.TupleNotTranslatedToOutput(tuple));
                }
            }

            this.AddIndexedCellTuples(customTablesById, cellsByTableAndRowId);
        }

        private bool IndexCustomTableTuple(WixCustomTableTuple wixCustomTableTuple, Dictionary<string, WixCustomTableTuple> customTablesById)
        {
            if (!wixCustomTableTuple.Unreal)
            {
                return false;
            }

            var tableId = wixCustomTableTuple.Id.Id;
            customTablesById.Add(tableId, wixCustomTableTuple);
            return true;
        }

        private void IndexCustomTableCellTuple(WixCustomTableCellTuple wixCustomTableCellTuple, Dictionary<string, List<WixCustomTableCellTuple>> cellsByTableAndRowId)
        {
            var tableAndRowId = wixCustomTableCellTuple.TableRef + "/" + wixCustomTableCellTuple.RowId;
            if (!cellsByTableAndRowId.TryGetValue(tableAndRowId, out var cells))
            {
                cells = new List<WixCustomTableCellTuple>();
                cellsByTableAndRowId.Add(tableAndRowId, cells);
            }

            cells.Add(wixCustomTableCellTuple);
        }

        private void AddIndexedCellTuples(Dictionary<string, WixCustomTableTuple> customTablesById, Dictionary<string, List<WixCustomTableCellTuple>> cellsByTableAndRowId)
        {
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, BurnBackendHelper.WriterSettings))
            {
                foreach (var rowOfCells in cellsByTableAndRowId.Values)
                {
                    var tableId = rowOfCells[0].TableRef;
                    var tableTuple = customTablesById[tableId];

                    if (!tableTuple.Unreal)
                    {
                        return;
                    }

                    var columnNames = tableTuple.ColumnNamesSeparated;

                    var rowDataByColumn = rowOfCells.ToDictionary(t => t.ColumnRef, t => t.Data);

                    writer.WriteStartElement(tableId, BurnCommon.BADataNamespace);

                    // Write all row data as attributes in table column order.
                    foreach (var column in columnNames)
                    {
                        if (rowDataByColumn.TryGetValue(column, out var data))
                        {
                            writer.WriteAttributeString(column, data);
                        }
                    }

                    writer.WriteEndElement();
                }
            }

            this.BackendHelper.AddBootstrapperApplicationData(sb.ToString());
        }

        private bool AddTupleFromExtension(IntermediateTuple tuple)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryAddTupleToDataManifest(this.Section, tuple))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
