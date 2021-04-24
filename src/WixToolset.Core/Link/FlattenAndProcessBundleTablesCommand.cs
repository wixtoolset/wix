// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class FlattenAndProcessBundleTablesCommand
    {
        public FlattenAndProcessBundleTablesCommand(IntermediateSection entrySection, IMessaging messaging)
        {
            this.EntrySection = entrySection;
            this.Messaging = messaging;
        }

        private IntermediateSection EntrySection { get; }

        private IMessaging Messaging { get; }

        public void Execute()
        {
            this.FlattenBundleTables();

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            this.ProcessBundleComplexReferences();
        }

        /// <summary>
        /// Flattens the tables used in a Bundle.
        /// </summary>
        private void FlattenBundleTables()
        {
            // We need to flatten the nested PayloadGroups and PackageGroups under
            // UX, Chain, and any Containers. When we're done, the WixGroups table
            // will hold Payloads under UX, ChainPackages (references?) under Chain,
            // and ContainerPackages/Payloads under any authored Containers.
            var groups = new WixGroupingOrdering(this.EntrySection, this.Messaging);

            // Create UX payloads and Package payloads and Container packages
            groups.UseTypes(new[] { ComplexReferenceParentType.Container, ComplexReferenceParentType.Layout, ComplexReferenceParentType.PackageGroup, ComplexReferenceParentType.PayloadGroup, ComplexReferenceParentType.Package },
                            new[] { ComplexReferenceChildType.ContainerPackage, ComplexReferenceChildType.PackageGroup, ComplexReferenceChildType.Package, ComplexReferenceChildType.PackagePayload, ComplexReferenceChildType.PayloadGroup, ComplexReferenceChildType.Payload });
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Package, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Container, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Layout, false);

            // Create Chain packages...
            groups.UseTypes(new[] { ComplexReferenceParentType.PackageGroup }, new[] { ComplexReferenceChildType.Package, ComplexReferenceChildType.PackageGroup });
            groups.FlattenAndRewriteRows(ComplexReferenceParentType.PackageGroup, "WixChain", false);

            groups.RemoveUsedGroupRows();
        }

        private void ProcessBundleComplexReferences()
        {
            var containersById = this.EntrySection.Symbols.OfType<WixBundleContainerSymbol>().ToDictionary(c => c.Id.Id);
            var groups = this.EntrySection.Symbols.OfType<WixGroupSymbol>().ToList();
            var payloadsById = this.EntrySection.Symbols.OfType<WixBundlePayloadSymbol>().ToDictionary(c => c.Id.Id);

            var containerByPackage = new Dictionary<string, WixBundleContainerSymbol>();
            var referencedPackages = new HashSet<string>();
            var payloadsInBA = new HashSet<string>();
            var payloadsInPackageOrLayout = new HashSet<string>();

            foreach (var groupSymbol in groups)
            {
                switch (groupSymbol.ChildType)
                {
                    case ComplexReferenceChildType.ContainerPackage:
                        switch (groupSymbol.ParentType)
                        {
                            case ComplexReferenceParentType.Container:
                                if (containerByPackage.TryGetValue(groupSymbol.ChildId, out var collisionContainer))
                                {
                                    this.Messaging.Write(LinkerErrors.PackageInMultipleContainers(groupSymbol.SourceLineNumbers, groupSymbol.ChildId, groupSymbol.ParentId, collisionContainer.Id.Id));
                                }
                                else
                                {
                                    containerByPackage.Add(groupSymbol.ChildId, containersById[groupSymbol.ParentId]);
                                }
                                break;
                        }
                        break;
                    case ComplexReferenceChildType.Package:
                        switch (groupSymbol.ParentType)
                        {
                            case ComplexReferenceParentType.PackageGroup:
                                if (groupSymbol.ParentId == "WixChain")
                                {
                                    referencedPackages.Add(groupSymbol.ChildId);
                                }
                                break;
                        }
                        break;
                    case ComplexReferenceChildType.Payload:
                        switch (groupSymbol.ParentType)
                        {
                            case ComplexReferenceParentType.Container:
                                if (groupSymbol.ParentId == BurnConstants.BurnUXContainerName)
                                {
                                    payloadsInBA.Add(groupSymbol.ChildId);
                                }
                                break;
                            case ComplexReferenceParentType.Layout:
                                payloadsById[groupSymbol.ChildId].LayoutOnly = true;
                                payloadsInPackageOrLayout.Add(groupSymbol.ChildId);
                                break;
                            case ComplexReferenceParentType.Package:
                                payloadsInPackageOrLayout.Add(groupSymbol.ChildId);
                                break;
                        }
                        break;
                }
            }

            foreach (var package in this.EntrySection.Symbols.OfType<WixBundlePackageSymbol>())
            {
                if (!referencedPackages.Contains(package.Id.Id))
                {
                    this.Messaging.Write(LinkerErrors.UnscheduledChainPackage(package.SourceLineNumbers, package.Id.Id));
                }
            }

            foreach (var rollbackBoundary in this.EntrySection.Symbols.OfType<WixBundleRollbackBoundarySymbol>())
            {
                if (!referencedPackages.Contains(rollbackBoundary.Id.Id))
                {
                    this.Messaging.Write(LinkerErrors.UnscheduledRollbackBoundary(rollbackBoundary.SourceLineNumbers, rollbackBoundary.Id.Id));
                }
            }

            foreach (var payload in payloadsById.Values)
            {
                var payloadId = payload.Id.Id;
                if (payloadsInBA.Contains(payloadId))
                {
                    if (payloadsInPackageOrLayout.Contains(payloadId))
                    {
                        this.Messaging.Write(LinkerErrors.PayloadSharedWithBA(payload.SourceLineNumbers, payloadId));
                    }
                }
                else if (!payloadsInPackageOrLayout.Contains(payloadId))
                {
                    this.Messaging.Write(LinkerErrors.OrphanedPayload(payload.SourceLineNumbers, payloadId));
                }
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Assign authored payloads to authored containers.
            // Compressed Payloads not assigned to a container here will get assigned to the default attached container during binding.
            foreach (var groupSymbol in groups)
            {
                if (groupSymbol.ChildType == ComplexReferenceChildType.Payload && groupSymbol.ParentType == ComplexReferenceParentType.Container)
                {
                    var payloadSymbol = payloadsById[groupSymbol.ChildId];
                    var containerId = groupSymbol.ParentId;

                    if (String.IsNullOrEmpty(payloadSymbol.ContainerRef))
                    {
                        payloadSymbol.ContainerRef = containerId;
                    }
                    else
                    {
                        this.Messaging.Write(LinkerWarnings.PayloadInMultipleContainers(groupSymbol.SourceLineNumbers, groupSymbol.ChildId, containerId, payloadSymbol.ContainerRef));
                    }

                    if (payloadSymbol.LayoutOnly)
                    {
                        this.Messaging.Write(LinkerWarnings.LayoutPayloadInContainer(groupSymbol.SourceLineNumbers, groupSymbol.ChildId, containerId));
                    }
                }
            }

        }
    }
}
