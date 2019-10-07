// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    internal class OrderPackagesAndRollbackBoundariesCommand
    {
        public OrderPackagesAndRollbackBoundariesCommand(IMessaging messaging, IEnumerable<WixGroupTuple> groupTuples, Dictionary<string, WixBundleRollbackBoundaryTuple> boundaryTuples, IDictionary<string, PackageFacade> packageFacades)
        {
            this.Messaging = messaging;
            this.GroupTuples = groupTuples;
            this.Boundaries = boundaryTuples;
            this.PackageFacades = packageFacades;
        }

        private IMessaging Messaging { get; }

        public IEnumerable<WixGroupTuple> GroupTuples { get; }

        public Dictionary<string, WixBundleRollbackBoundaryTuple> Boundaries { get; }

        public IDictionary<string, PackageFacade> PackageFacades { get; }

        public IEnumerable<PackageFacade> OrderedPackageFacades { get; private set; }

        public IEnumerable<WixBundleRollbackBoundaryTuple> UsedRollbackBoundaries { get; private set; }

        public void Execute()
        {
            var orderedFacades = new List<PackageFacade>();
            var usedBoundaries = new List<WixBundleRollbackBoundaryTuple>();

            // Process the chain of packages to add them in the correct order
            // and assign the forward rollback boundaries as appropriate. Remember
            // rollback boundaries are authored as elements in the chain which
            // we re-interpret here to add them as attributes on the next available
            // package in the chain. Essentially we mark some packages as being
            // the start of a rollback boundary when installing and repairing.
            // We handle uninstall (aka: backwards) rollback boundaries after
            // we get these install/repair (aka: forward) rollback boundaries
            // defined.
            WixBundleRollbackBoundaryTuple previousRollbackBoundary = null;
            WixBundleRollbackBoundaryTuple lastRollbackBoundary = null;
            var boundaryHadX86Package = false;

            foreach (var groupTuple in this.GroupTuples)
            {
                if (ComplexReferenceChildType.Package == groupTuple.ChildType && ComplexReferenceParentType.PackageGroup == groupTuple.ParentType && "WixChain" == groupTuple.ParentId)
                {
                    if (this.PackageFacades.TryGetValue(groupTuple.ChildId, out var facade))
                    {
                        if (null != previousRollbackBoundary)
                        {
                            usedBoundaries.Add(previousRollbackBoundary);
                            facade.PackageTuple.RollbackBoundaryRef = previousRollbackBoundary.Id.Id;
                            previousRollbackBoundary = null;

                            boundaryHadX86Package = facade.PackageTuple.Win64;
                        }

                        // Error if MSI transaction has x86 package preceding x64 packages
                        if ((lastRollbackBoundary != null)
                            && lastRollbackBoundary.Transaction == true
                            && boundaryHadX86Package 
                            && facade.PackageTuple.Win64)
                        {
                            this.Messaging.Write(ErrorMessages.MsiTransactionX86BeforeX64(lastRollbackBoundary.SourceLineNumbers));
                        }
                        boundaryHadX86Package |= facade.PackageTuple.Win64;

                        orderedFacades.Add(facade);
                    }
                    else // must be a rollback boundary.
                    {
                        // Discard the next rollback boundary if we have a previously defined boundary.
                        var nextRollbackBoundary = this.Boundaries[groupTuple.ChildId];
                        if (null != previousRollbackBoundary)
                        {
                            this.Messaging.Write(WarningMessages.DiscardedRollbackBoundary(nextRollbackBoundary.SourceLineNumbers, nextRollbackBoundary.Id.Id));
                        }
                        else
                        {
                            previousRollbackBoundary = nextRollbackBoundary;
                            lastRollbackBoundary = nextRollbackBoundary;
                        }
                    }
                }
            }

            if (null != previousRollbackBoundary)
            {
                this.Messaging.Write(WarningMessages.DiscardedRollbackBoundary(previousRollbackBoundary.SourceLineNumbers, previousRollbackBoundary.Id.Id));
            }

            // With the forward rollback boundaries assigned, we can now go
            // through the packages with rollback boundaries and assign backward
            // rollback boundaries. Backward rollback boundaries are used when
            // the chain is going "backwards" which (AFAIK) only happens during
            // uninstall.
            //
            // Consider the scenario with three packages: A, B and C. Packages A
            // and C are marked as rollback boundary packages and package B is
            // not. The naive implementation would execute the chain like this
            // (numbers indicate where rollback boundaries would end up):
            //      install:    1 A B 2 C
            //      uninstall:  2 C B 1 A
            //
            // The uninstall chain is wrong, A and B should be grouped together
            // not C and B. The fix is to label packages with a "backwards"
            // rollback boundary used during uninstall. The backwards rollback
            // boundaries are assigned to the package *before* the next rollback
            // boundary. Using our example from above again, I'll mark the
            // backwards rollback boundaries prime (aka: with ').
            //      install:    1 A B 1' 2 C 2'
            //      uninstall:  2' C 2 1' B A 1
            //
            // If the marked boundaries are ignored during install you get the
            // same thing as above (good) and if the non-marked boundaries are
            // ignored during uninstall then A and B are correctly grouped.
            // Here's what it looks like without all the markers:
            //      install:    1 A B 2 C
            //      uninstall:  2 C 1 B A
            // Woot!
            string previousRollbackBoundaryId = null;
            PackageFacade previousFacade = null;

            foreach (PackageFacade package in orderedFacades)
            {
                if (null != package.PackageTuple.RollbackBoundaryRef)
                {
                    if (null != previousFacade)
                    {
                        previousFacade.PackageTuple.RollbackBoundaryBackwardRef = previousRollbackBoundaryId;
                    }

                    previousRollbackBoundaryId = package.PackageTuple.RollbackBoundaryRef;
                }

                previousFacade = package;
            }

            if (!String.IsNullOrEmpty(previousRollbackBoundaryId) && null != previousFacade)
            {
                previousFacade.PackageTuple.RollbackBoundaryBackwardRef = previousRollbackBoundaryId;
            }

            this.OrderedPackageFacades = orderedFacades;
            this.UsedRollbackBoundaries = usedBoundaries;
        }
    }
}
