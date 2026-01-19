// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolves all the simple references in a section.
    /// </summary>
    internal class ResolveReferencesCommand
    {
        private readonly IntermediateSection entrySection;
        private readonly IDictionary<string, SymbolWithSection> symbolsWithSections;
        private HashSet<SymbolWithSection> referencedSymbols;
        private HashSet<IntermediateSection> resolvedSections;

        public ResolveReferencesCommand(IMessaging messaging, IntermediateSection entrySection, IDictionary<string, SymbolWithSection> symbolsWithSections)
        {
            this.Messaging = messaging;
            this.entrySection = entrySection;
            this.symbolsWithSections = symbolsWithSections;
            this.BuildingMergeModule = (SectionType.Module == entrySection.Type);
        }

        public IReadOnlyCollection<SymbolWithSection> ReferencedSymbolWithSections => this.referencedSymbols;

        public ISet<IntermediateSection> ResolvedSections => this.resolvedSections;

        private bool BuildingMergeModule { get; }

        private IMessaging Messaging { get; }

        /// <summary>
        /// Resolves all the simple references in a section.
        /// </summary>
        public void Execute()
        {
            this.resolvedSections = new HashSet<IntermediateSection>();
            this.referencedSymbols = new HashSet<SymbolWithSection>();

            this.RecursivelyResolveReferences(this.entrySection);
        }

        /// <summary>
        /// Recursive helper function to resolve all references of passed in section.
        /// </summary>
        /// <param name="section">Section with references to resolve.</param>
        /// <remarks>Note: recursive function.</remarks>
        private void RecursivelyResolveReferences(IntermediateSection section)
        {
            // If we already resolved this section, move on to the next.
            if (!this.resolvedSections.Add(section))
            {
                return;
            }

            // Process all of the references contained in this section using the collection of
            // symbols provided.  Then recursively call this method to process the
            // located symbol's section.  All in all this is a very simple depth-first
            // search of the references per-section.
            foreach (var reference in section.Symbols.OfType<WixSimpleReferenceSymbol>())
            {
                // If we're building a Merge Module, ignore all references to the Media table
                // because Merge Modules don't have Media tables.
                if (this.BuildingMergeModule && reference.Table == "Media")
                {
                    continue;
                }

                // See if the symbol (and any of its duplicates) are appropriately accessible.
                if (this.symbolsWithSections.TryGetValue(reference.SymbolicName, out var symbolWithSection))
                {
                    var accessible = this.DetermineAccessibleSymbols(section, symbolWithSection);

                    if (accessible.Count == 1)
                    {
                        var accessibleSymbol = accessible[0];

                        accessibleSymbol.AddDirectReference(reference);

                        if (this.referencedSymbols.Add(accessibleSymbol) && null != accessibleSymbol.Section)
                        {
                            this.RecursivelyResolveReferences(accessibleSymbol.Section);
                        }
                    }
                    else if (accessible.Count == 0)
                    {
                        this.Messaging.Write(CoreErrors.UnresolvedReference(reference.SourceLineNumbers, reference.SymbolicName, symbolWithSection.Access));
                    }
                    else // multiple accessible symbols referenced creates conflicting symbols.
                    {
                        // Remember the direct reference to the symbol for the error reporting later,
                        // but do NOT continue resolving references found in these conflicting symbols.
                        foreach (var conflict in accessible)
                        {
                            // This should NEVER happen.
                            if (!conflict.PossiblyConflicts.Any())
                            {
                                throw new InvalidOperationException("If a reference can reference multiple symbols, those symbols MUST have already been recognized as possible conflicts.");
                            }

                            conflict.AddDirectReference(reference);

                            this.referencedSymbols.Add(conflict);

                            if (conflict.Section != null)
                            {
                                this.resolvedSections.Add(conflict.Section);
                            }
                        }
                    }
                }
                else
                {
                    this.Messaging.Write(CoreErrors.UnresolvedReference(reference.SourceLineNumbers, reference.SymbolicName));
                }
            }
        }

        /// <summary>
        /// Determine if the symbol and any of its duplicates are accessbile by the referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the symbol.</param>
        /// <param name="symbolWithSection">Symbol being referenced.</param>
        /// <returns>List of symbols accessible by referencing section.</returns>
        private List<SymbolWithSection> DetermineAccessibleSymbols(IntermediateSection referencingSection, SymbolWithSection symbolWithSection)
        {
            var accessibleSymbols = new List<SymbolWithSection>();
            List<SymbolWithSection> virtualSymbols = null;

            if (this.AccessibleSymbol(referencingSection, symbolWithSection))
            {
                AddSymbolToCorrectCollection(symbolWithSection, accessibleSymbols, ref virtualSymbols);
            }

            foreach (var dupe in symbolWithSection.PossiblyConflicts)
            {
                if (this.AccessibleSymbol(referencingSection, dupe))
                {
                    AddSymbolToCorrectCollection(dupe, accessibleSymbols, ref virtualSymbols);
                }
            }

            // If a virtual symbol is accessible we have some extra work to do.
            if (virtualSymbols != null)
            {
                // If there are multiple virtual symbols accessible that's an error case so add them all and
                // they'll be reported as conflicts later.
                if (virtualSymbols.Count > 1)
                {
                    accessibleSymbols.AddRange(virtualSymbols);
                }
                else
                {
                    var overrode = false;

                    // If there are any override symbols, skip adding the virtual symbols because they are "overridden".
                    foreach (var overrideSymbol in accessibleSymbols.Where(symbol => symbol.Access == AccessModifier.Override))
                    {
                        overrideSymbol.OverrideVirtualSymbol(virtualSymbols[0]);

                        overrode = true;
                    }

                    // If there are no overriding symbols, add the virtual symbol (there is only one but we'll add the collection just in case)
                    // so it will be reported as a conflict later.
                    if (!overrode)
                    {
                        accessibleSymbols.AddRange(virtualSymbols);
                    }
                }
            }

            return accessibleSymbols;
        }

        /// <summary>
        /// Add a symbol to the correct collection based on its access.
        /// </summary>
        /// <param name="symbolWithSection"></param>
        /// <param name="accessibleSymbols">Collection of non-virtual symbol that was accessible.</param>
        /// <param name="virtualSymbols">Collection of virtual symbol that was accessible</param>
        private static void AddSymbolToCorrectCollection(SymbolWithSection symbolWithSection, List<SymbolWithSection> accessibleSymbols, ref List<SymbolWithSection> virtualSymbols)
        {
            if (symbolWithSection.Access == AccessModifier.Virtual)
            {
                if (virtualSymbols == null)
                {
                    virtualSymbols = new List<SymbolWithSection>();
                }

                virtualSymbols.Add(symbolWithSection);
            }
            else
            {
                accessibleSymbols.Add(symbolWithSection);
            }
        }

        /// <summary>
        /// Determine if a single symbol is accessible by the referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the symbol.</param>
        /// <param name="symbolWithSection">Symbol being referenced.</param>
        /// <returns>True if symbol is accessible.</returns>
        private bool AccessibleSymbol(IntermediateSection referencingSection, SymbolWithSection symbolWithSection)
        {
            switch (symbolWithSection.Access)
            {
                case AccessModifier.Global:
                case AccessModifier.Virtual:
                case AccessModifier.Override:
                    return true;
                case AccessModifier.Library:
                    return symbolWithSection.Section.CompilationId == referencingSection.CompilationId || (null != symbolWithSection.Section.LibraryId && symbolWithSection.Section.LibraryId == referencingSection.LibraryId);
                case AccessModifier.File:
                    return symbolWithSection.Section.CompilationId == referencingSection.CompilationId;
                case AccessModifier.Section:
                    return referencingSection == symbolWithSection.Section;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbolWithSection.Access));
            }
        }
    }
}
