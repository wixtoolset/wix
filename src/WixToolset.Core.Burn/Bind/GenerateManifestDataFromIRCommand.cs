// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
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
            var cellsByCustomDataAndElementId = new Dictionary<string, List<WixBundleCustomDataCellTuple>>();
            var customDataById = new Dictionary<string, WixBundleCustomDataTuple>();

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
                    case TupleDefinitionType.WixBundleCustomDataAttribute:
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
                    case TupleDefinitionType.WixBundlePackageGroup:
                    case TupleDefinitionType.WixBundlePatchTargetCode:
                    case TupleDefinitionType.WixBundlePayload:
                    case TupleDefinitionType.WixBundlePayloadGroup:
                    case TupleDefinitionType.WixBundleRelatedPackage:
                    case TupleDefinitionType.WixBundleRollbackBoundary:
                    case TupleDefinitionType.WixBundleSlipstreamMsp:
                    case TupleDefinitionType.WixBundleUpdate:
                    case TupleDefinitionType.WixBundleVariable:
                    case TupleDefinitionType.WixBuildInfo:
                    case TupleDefinitionType.WixChain:
                    case TupleDefinitionType.WixComponentSearch:
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

                    // Tuples used before binding.
                    case TupleDefinitionType.WixComplexReference:
                    case TupleDefinitionType.WixOrdering:
                    case TupleDefinitionType.WixSimpleReference:
                    case TupleDefinitionType.WixVariable:
                        break;

                    // Tuples to investigate:
                    case TupleDefinitionType.WixChainItem:
                        break;

                    case TupleDefinitionType.WixBundleCustomData:
                        unknownTuple = !this.IndexBundleCustomDataTuple((WixBundleCustomDataTuple)tuple, customDataById);
                        break;

                    case TupleDefinitionType.WixBundleCustomDataCell:
                        this.IndexBundleCustomDataCellTuple((WixBundleCustomDataCellTuple)tuple, cellsByCustomDataAndElementId);
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

            this.AddIndexedCellTuples(customDataById, cellsByCustomDataAndElementId);
        }

        private bool IndexBundleCustomDataTuple(WixBundleCustomDataTuple wixBundleCustomDataTuple, Dictionary<string, WixBundleCustomDataTuple> customDataById)
        {
            switch (wixBundleCustomDataTuple.Type)
            {
                case WixBundleCustomDataType.BootstrapperApplication:
                case WixBundleCustomDataType.BundleExtension:
                    break;
                default:
                    return false;
            }

            var customDataId = wixBundleCustomDataTuple.Id.Id;
            customDataById.Add(customDataId, wixBundleCustomDataTuple);
            return true;
        }

        private void IndexBundleCustomDataCellTuple(WixBundleCustomDataCellTuple wixBundleCustomDataCellTuple, Dictionary<string, List<WixBundleCustomDataCellTuple>> cellsByCustomDataAndElementId)
        {
            var tableAndRowId = wixBundleCustomDataCellTuple.CustomDataRef + "/" + wixBundleCustomDataCellTuple.ElementId;
            if (!cellsByCustomDataAndElementId.TryGetValue(tableAndRowId, out var cells))
            {
                cells = new List<WixBundleCustomDataCellTuple>();
                cellsByCustomDataAndElementId.Add(tableAndRowId, cells);
            }

            cells.Add(wixBundleCustomDataCellTuple);
        }

        private void AddIndexedCellTuples(Dictionary<string, WixBundleCustomDataTuple> customDataById, Dictionary<string, List<WixBundleCustomDataCellTuple>> cellsByCustomDataAndElementId)
        {
            foreach (var elementValues in cellsByCustomDataAndElementId.Values)
            {
                var elementName = elementValues[0].CustomDataRef;
                var customDataTuple = customDataById[elementName];

                var attributeNames = customDataTuple.AttributeNamesSeparated;

                var elementValuesByAttribute = elementValues.ToDictionary(t => t.AttributeRef, t => t.Value);

                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, BurnBackendHelper.WriterSettings))
                {
                    switch (customDataTuple.Type)
                    {
                        case WixBundleCustomDataType.BootstrapperApplication:
                            writer.WriteStartElement(elementName, BurnCommon.BADataNamespace);
                            break;
                        case WixBundleCustomDataType.BundleExtension:
                            writer.WriteStartElement(elementName, BurnCommon.BundleExtensionDataNamespace);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    // Write all row data as attributes in table column order.
                    foreach (var attributeName in attributeNames)
                    {
                        if (elementValuesByAttribute.TryGetValue(attributeName, out var value))
                        {
                            writer.WriteAttributeString(attributeName, value);
                        }
                    }

                    writer.WriteEndElement();
                }

                switch (customDataTuple.Type)
                {
                    case WixBundleCustomDataType.BootstrapperApplication:
                        this.BackendHelper.AddBootstrapperApplicationData(sb.ToString());
                        break;
                    case WixBundleCustomDataType.BundleExtension:
                        this.BackendHelper.AddBundleExtensionData(customDataTuple.BundleExtensionRef, sb.ToString());
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
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
