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
            // and ChainPackages/Payloads under the attached and any detatched
            // Containers.
            var groups = new WixGroupingOrdering(this.EntrySection, this.Messaging);

            // Create UX payloads and Package payloads
            groups.UseTypes(new[] { ComplexReferenceParentType.Container, ComplexReferenceParentType.Layout, ComplexReferenceParentType.PackageGroup, ComplexReferenceParentType.PayloadGroup, ComplexReferenceParentType.Package },
                            new[] { ComplexReferenceChildType.PackageGroup, ComplexReferenceChildType.Package, ComplexReferenceChildType.PackagePayload, ComplexReferenceChildType.PayloadGroup, ComplexReferenceChildType.Payload });
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Package, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Container, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Layout, false);

            // Create Chain packages...
            groups.UseTypes(new[] { ComplexReferenceParentType.PackageGroup }, new[] { ComplexReferenceChildType.Package, ComplexReferenceChildType.PackageGroup });
            groups.FlattenAndRewriteRows(ComplexReferenceChildType.PackageGroup, "WixChain", false);

            groups.RemoveUsedGroupRows();
        }

        private void ProcessBundleComplexReferences()
        {
            var groups = this.EntrySection.Symbols.OfType<WixGroupSymbol>().ToList();
            var payloadsById = this.EntrySection.Symbols.OfType<WixBundlePayloadSymbol>().ToDictionary(c => c.Id.Id);

            // Assign authored payloads to authored containers.
            // Compressed Payloads not assigned to a container here will get assigned to the default attached container during binding.
            foreach (var groupSymbol in groups)
            {
                if (ComplexReferenceChildType.Payload == groupSymbol.ChildType)
                {
                    var payloadSymbol = payloadsById[groupSymbol.ChildId];

                    if (ComplexReferenceParentType.Container == groupSymbol.ParentType)
                    {
                        // TODO: v3 didn't warn if we overwrote the payload's container.
                        // Should we warn now?
                        payloadSymbol.ContainerRef = groupSymbol.ParentId;
                    }
                    else if (ComplexReferenceParentType.Layout == groupSymbol.ParentType)
                    {
                        payloadSymbol.LayoutOnly = true;
                    }
                }
            }

        }
    }
}
