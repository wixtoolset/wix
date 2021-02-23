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
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class GenerateManifestDataFromIRCommand
    {
        public GenerateManifestDataFromIRCommand(IMessaging messaging, IntermediateSection section, IEnumerable<IBurnBackendBinderExtension> backendExtensions, IBurnBackendHelper backendHelper, IDictionary<string, IList<IntermediateSymbol>> extensionSearchSymbolsById)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.BackendExtensions = backendExtensions;
            this.BackendHelper = backendHelper;
            this.ExtensionSearchSymbolsById = extensionSearchSymbolsById;
        }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private IBurnBackendHelper BackendHelper { get; }

        private IDictionary<string, IList<IntermediateSymbol>> ExtensionSearchSymbolsById { get; }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var symbols = this.Section.Symbols.ToList();
            var cellsByCustomDataAndElementId = new Dictionary<string, List<WixBundleCustomDataCellSymbol>>();
            var customDataById = new Dictionary<string, WixBundleCustomDataSymbol>();

            foreach (var kvp in this.ExtensionSearchSymbolsById)
            {
                var extensionId = kvp.Key;
                var extensionSearchSymbols = kvp.Value;
                foreach (var extensionSearchSymbol in extensionSearchSymbols)
                {
                    this.BackendHelper.AddBundleExtensionData(extensionId, extensionSearchSymbol, symbolIdIsIdAttribute: true);
                    symbols.Remove(extensionSearchSymbol);
                }
            }

            foreach (var symbol in symbols)
            {
                var unknownSymbol = false;
                switch (symbol.Definition.Type)
                {
                    // Symbols used internally and are not added to a data manifest.
                    case SymbolDefinitionType.ProvidesDependency:
                    case SymbolDefinitionType.WixApprovedExeForElevation:
                    case SymbolDefinitionType.WixBootstrapperApplication:
                    case SymbolDefinitionType.WixBootstrapperApplicationDll:
                    case SymbolDefinitionType.WixBundle:
                    case SymbolDefinitionType.WixBundleContainer:
                    case SymbolDefinitionType.WixBundleCustomDataAttribute:
                    case SymbolDefinitionType.WixBundleExePackage:
                    case SymbolDefinitionType.WixBundleExtension:
                    case SymbolDefinitionType.WixBundleMsiFeature:
                    case SymbolDefinitionType.WixBundleMsiPackage:
                    case SymbolDefinitionType.WixBundleMsiProperty:
                    case SymbolDefinitionType.WixBundleMspPackage:
                    case SymbolDefinitionType.WixBundleMsuPackage:
                    case SymbolDefinitionType.WixBundlePackage:
                    case SymbolDefinitionType.WixBundlePackageCommandLine:
                    case SymbolDefinitionType.WixBundlePackageExitCode:
                    case SymbolDefinitionType.WixBundlePackageGroup:
                    case SymbolDefinitionType.WixBundlePatchTargetCode:
                    case SymbolDefinitionType.WixBundlePayload:
                    case SymbolDefinitionType.WixBundlePayloadGroup:
                    case SymbolDefinitionType.WixBundleRelatedPackage:
                    case SymbolDefinitionType.WixBundleRollbackBoundary:
                    case SymbolDefinitionType.WixBundleSlipstreamMsp:
                    case SymbolDefinitionType.WixBundleUpdate:
                    case SymbolDefinitionType.WixBundleVariable:
                    case SymbolDefinitionType.WixBuildInfo:
                    case SymbolDefinitionType.WixChain:
                    case SymbolDefinitionType.WixComponentSearch:
                    case SymbolDefinitionType.WixDependencyProvider:
                    case SymbolDefinitionType.WixFileSearch:
                    case SymbolDefinitionType.WixGroup:
                    case SymbolDefinitionType.WixProductSearch:
                    case SymbolDefinitionType.WixRegistrySearch:
                    case SymbolDefinitionType.WixRelatedBundle:
                    case SymbolDefinitionType.WixSearch:
                    case SymbolDefinitionType.WixSearchRelation:
                    case SymbolDefinitionType.WixSetVariable:
                    case SymbolDefinitionType.WixUpdateRegistration:
                        break;

                    // Symbols used before binding.
                    case SymbolDefinitionType.WixComplexReference:
                    case SymbolDefinitionType.WixOrdering:
                    case SymbolDefinitionType.WixSimpleReference:
                    case SymbolDefinitionType.WixVariable:
                        break;

                    // Symbols to investigate:
                    case SymbolDefinitionType.WixChainItem:
                        break;

                    case SymbolDefinitionType.WixBundleCustomData:
                        unknownSymbol = !this.IndexBundleCustomDataSymbol((WixBundleCustomDataSymbol)symbol, customDataById);
                        break;

                    case SymbolDefinitionType.WixBundleCustomDataCell:
                        this.IndexBundleCustomDataCellSymbol((WixBundleCustomDataCellSymbol)symbol, cellsByCustomDataAndElementId);
                        break;

                    case SymbolDefinitionType.MustBeFromAnExtension:
                        unknownSymbol = !this.AddSymbolFromExtension(symbol);
                        break;

                    default:
                        unknownSymbol = true;
                        break;
                }

                if (unknownSymbol)
                {
                    this.Messaging.Write(WarningMessages.SymbolNotTranslatedToOutput(symbol));
                }
            }

            this.AddIndexedCellSymbols(customDataById, cellsByCustomDataAndElementId);
        }

        private bool IndexBundleCustomDataSymbol(WixBundleCustomDataSymbol wixBundleCustomDataSymbol, Dictionary<string, WixBundleCustomDataSymbol> customDataById)
        {
            switch (wixBundleCustomDataSymbol.Type)
            {
                case WixBundleCustomDataType.BootstrapperApplication:
                case WixBundleCustomDataType.BundleExtension:
                    break;
                default:
                    return false;
            }

            var customDataId = wixBundleCustomDataSymbol.Id.Id;
            customDataById.Add(customDataId, wixBundleCustomDataSymbol);
            return true;
        }

        private void IndexBundleCustomDataCellSymbol(WixBundleCustomDataCellSymbol wixBundleCustomDataCellSymbol, Dictionary<string, List<WixBundleCustomDataCellSymbol>> cellsByCustomDataAndElementId)
        {
            var tableAndRowId = wixBundleCustomDataCellSymbol.CustomDataRef + "/" + wixBundleCustomDataCellSymbol.ElementId;
            if (!cellsByCustomDataAndElementId.TryGetValue(tableAndRowId, out var cells))
            {
                cells = new List<WixBundleCustomDataCellSymbol>();
                cellsByCustomDataAndElementId.Add(tableAndRowId, cells);
            }

            cells.Add(wixBundleCustomDataCellSymbol);
        }

        private void AddIndexedCellSymbols(Dictionary<string, WixBundleCustomDataSymbol> customDataById, Dictionary<string, List<WixBundleCustomDataCellSymbol>> cellsByCustomDataAndElementId)
        {
            foreach (var elementValues in cellsByCustomDataAndElementId.Values)
            {
                var elementName = elementValues[0].CustomDataRef;
                var customDataSymbol = customDataById[elementName];

                var attributeNames = customDataSymbol.AttributeNamesSeparated;

                var elementValuesByAttribute = elementValues.ToDictionary(t => t.AttributeRef, t => t.Value);

                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, BurnBackendHelper.WriterSettings))
                {
                    switch (customDataSymbol.Type)
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

                switch (customDataSymbol.Type)
                {
                    case WixBundleCustomDataType.BootstrapperApplication:
                        this.BackendHelper.AddBootstrapperApplicationData(sb.ToString());
                        break;
                    case WixBundleCustomDataType.BundleExtension:
                        this.BackendHelper.AddBundleExtensionData(customDataSymbol.BundleExtensionRef, sb.ToString());
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private bool AddSymbolFromExtension(IntermediateSymbol symbol)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryProcessSymbol(this.Section, symbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
