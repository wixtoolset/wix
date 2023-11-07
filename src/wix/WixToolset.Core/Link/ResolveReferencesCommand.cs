// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public IEnumerable<SymbolWithSection> ReferencedSymbolWithSections => this.referencedSymbols;

        public IEnumerable<IntermediateSection> ResolvedSections => this.resolvedSections;

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
            foreach (var wixSimpleReferenceRow in section.Symbols.OfType<WixSimpleReferenceSymbol>())
            {
                // If we're building a Merge Module, ignore all references to the Media table
                // because Merge Modules don't have Media tables.
                if (this.BuildingMergeModule && wixSimpleReferenceRow.Table == "Media")
                {
                    continue;
                }

                // See if the symbol (and any of its duplicates) are appropriately accessible.
                if (this.symbolsWithSections.TryGetValue(wixSimpleReferenceRow.SymbolicName, out var symbolWithSection))
                {
                    var accessible = this.DetermineAccessibleSymbols(section, symbolWithSection);
                    if (accessible.Count == 1)
                    {
                        var accessibleSymbol = accessible[0];
                        if (this.referencedSymbols.Add(accessibleSymbol) && null != accessibleSymbol.Section)
                        {
                            this.RecursivelyResolveReferences(accessibleSymbol.Section);
                        }
                    }
                    else if (accessible.Count == 0)
                    {
                        this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName, symbolWithSection.Access));
                    }
                    else // display errors for the duplicate symbols.
                    {
                        var accessibleSymbol = accessible[0];
                        var accessibleFullName = accessibleSymbol.GetFullName();
                        var referencingSourceLineNumber = wixSimpleReferenceRow.SourceLineNumbers?.ToString();

                        if (String.IsNullOrEmpty(referencingSourceLineNumber))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleSymbol.Symbol.SourceLineNumbers, accessibleFullName));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleSymbol.Symbol.SourceLineNumbers, accessibleFullName, referencingSourceLineNumber));
                        }

                        foreach (var accessibleDuplicate in accessible.Skip(1))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol2(accessibleDuplicate.Symbol.SourceLineNumbers));
                        }
                    }
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName));
                }
            }
        }

        /// <summary>
        /// Determine if the symbol and any of its duplicates are accessbile by referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the symbol.</param>
        /// <param name="symbolWithSection">Symbol being referenced.</param>
        /// <returns>List of symbols accessible by referencing section.</returns>
        private List<SymbolWithSection> DetermineAccessibleSymbols(IntermediateSection referencingSection, SymbolWithSection symbolWithSection)
        {
            var accessibleSymbols = new List<SymbolWithSection>();

            if (this.AccessibleSymbol(referencingSection, symbolWithSection))
            {
                accessibleSymbols.Add(symbolWithSection);
            }

            foreach (var dupe in symbolWithSection.PossiblyConflicts)
            {
                // don't count overridable WixActionSymbols
                var symbolAction = symbolWithSection.Symbol as WixActionSymbol;
                var dupeAction = dupe.Symbol as WixActionSymbol;
                if (symbolAction?.Overridable != dupeAction?.Overridable)
                {
                    continue;
                }

                if (this.AccessibleSymbol(referencingSection, dupe))
                {
                    accessibleSymbols.Add(dupe);
                }
            }

            return accessibleSymbols;
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
