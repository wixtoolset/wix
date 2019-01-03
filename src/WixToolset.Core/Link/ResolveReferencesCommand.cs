// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolves all the simple references in a section.
    /// </summary>
    internal class ResolveReferencesCommand
    {
        private IntermediateSection entrySection;
        private IDictionary<string, Symbol> symbols;
        private HashSet<Symbol> referencedSymbols;
        private HashSet<IntermediateSection> resolvedSections;

        public ResolveReferencesCommand(IMessaging messaging, IntermediateSection entrySection, IDictionary<string, Symbol> symbols)
        {
            this.Messaging = messaging;
            this.entrySection = entrySection;
            this.symbols = symbols;
        }

        public bool BuildingMergeModule { private get; set; }

        public IEnumerable<Symbol> ReferencedSymbols { get { return this.referencedSymbols; } }

        public IEnumerable<IntermediateSection> ResolvedSections { get { return this.resolvedSections; } }

        private IMessaging Messaging { get; }

        /// <summary>
        /// Resolves all the simple references in a section.
        /// </summary>
        public void Execute()
        {
            this.resolvedSections = new HashSet<IntermediateSection>();
            this.referencedSymbols = new HashSet<Symbol>();

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
            foreach (var wixSimpleReferenceRow in section.Tuples.OfType<WixSimpleReferenceTuple>())
            {
                // If we're building a Merge Module, ignore all references to the Media table
                // because Merge Modules don't have Media tables.
                if (this.BuildingMergeModule && wixSimpleReferenceRow.Table== "Media")
                {
                    continue;
                }

                if (!this.symbols.TryGetValue(wixSimpleReferenceRow.SymbolicName, out var symbol))
                {
                    this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName));
                }
                else // see if the symbol (and any of its duplicates) are appropriately accessible.
                {
                    IList<Symbol> accessible = DetermineAccessibleSymbols(section, symbol);
                    if (!accessible.Any())
                    {
                        this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName, symbol.Access));
                    }
                    else if (1 == accessible.Count)
                    {
                        var accessibleSymbol = accessible[0];
                        this.referencedSymbols.Add(accessibleSymbol);

                        if (null != accessibleSymbol.Section)
                        {
                            RecursivelyResolveReferences(accessibleSymbol.Section);
                        }
                    }
                    else // display errors for the duplicate symbols.
                    {
                        var accessibleSymbol = accessible[0];
                        var referencingSourceLineNumber = wixSimpleReferenceRow.SourceLineNumbers?.ToString();

                        if (String.IsNullOrEmpty(referencingSourceLineNumber))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleSymbol.Row.SourceLineNumbers, accessibleSymbol.Name));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleSymbol.Row.SourceLineNumbers, accessibleSymbol.Name, referencingSourceLineNumber));
                        }

                        foreach (Symbol accessibleDuplicate in accessible.Skip(1))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol2(accessibleDuplicate.Row.SourceLineNumbers));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the symbol and any of its duplicates are accessbile by referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the symbol.</param>
        /// <param name="symbol">Symbol being referenced.</param>
        /// <returns>List of symbols accessible by referencing section.</returns>
        private IList<Symbol> DetermineAccessibleSymbols(IntermediateSection referencingSection, Symbol symbol)
        {
            List<Symbol> symbols = new List<Symbol>();

            if (AccessibleSymbol(referencingSection, symbol))
            {
                symbols.Add(symbol);
            }

            foreach (Symbol dupe in symbol.PossiblyConflictingSymbols)
            {
                if (AccessibleSymbol(referencingSection, dupe))
                {
                    symbols.Add(dupe);
                }
            }

            foreach (Symbol dupe in symbol.RedundantSymbols)
            {
                if (AccessibleSymbol(referencingSection, dupe))
                {
                    symbols.Add(dupe);
                }
            }

            return symbols;
        }

        /// <summary>
        /// Determine if a single symbol is accessible by the referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the symbol.</param>
        /// <param name="symbol">Symbol being referenced.</param>
        /// <returns>True if symbol is accessible.</returns>
        private bool AccessibleSymbol(IntermediateSection referencingSection, Symbol symbol)
        {
            switch (symbol.Access)
            {
                case AccessModifier.Public:
                    return true;
                case AccessModifier.Internal:
                    return symbol.Section.CompilationId.Equals(referencingSection.CompilationId) || (null != symbol.Section.LibraryId && symbol.Section.LibraryId.Equals(referencingSection.LibraryId));
                case AccessModifier.Protected:
                    return symbol.Section.CompilationId.Equals(referencingSection.CompilationId);
                case AccessModifier.Private:
                    return referencingSection == symbol.Section;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol.Access));
            }
        }
    }
}
