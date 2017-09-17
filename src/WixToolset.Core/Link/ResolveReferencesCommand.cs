// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Link
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Resolves all the simple references in a section.
    /// </summary>
    internal class ResolveReferencesCommand : ICommand
    {
        private Section entrySection;
        private IDictionary<string, Symbol> symbols;
        private HashSet<Symbol> referencedSymbols;
        private HashSet<Section> resolvedSections;

        public ResolveReferencesCommand(Section entrySection, IDictionary<string, Symbol> symbols)
        {
            this.entrySection = entrySection;
            this.symbols = symbols;
        }

        public bool BuildingMergeModule { private get; set; }

        public IEnumerable<Symbol> ReferencedSymbols { get { return this.referencedSymbols; } }

        public IEnumerable<Section> ResolvedSections { get { return this.resolvedSections; } }

        /// <summary>
        /// Resolves all the simple references in a section.
        /// </summary>
        public void Execute()
        {
            this.resolvedSections = new HashSet<Section>();
            this.referencedSymbols = new HashSet<Symbol>();

            this.RecursivelyResolveReferences(this.entrySection);
        }

        /// <summary>
        /// Recursive helper function to resolve all references of passed in section.
        /// </summary>
        /// <param name="section">Section with references to resolve.</param>
        /// <remarks>Note: recursive function.</remarks>
        private void RecursivelyResolveReferences(Section section)
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
            Table wixSimpleReferenceTable;
            if (section.Tables.TryGetTable("WixSimpleReference", out wixSimpleReferenceTable))
            {
                foreach (WixSimpleReferenceRow wixSimpleReferenceRow in wixSimpleReferenceTable.Rows)
                {
                    Debug.Assert(wixSimpleReferenceRow.Section == section);

                    // If we're building a Merge Module, ignore all references to the Media table
                    // because Merge Modules don't have Media tables.
                    if (this.BuildingMergeModule && "Media" == wixSimpleReferenceRow.TableName)
                    {
                        continue;
                    }

                    Symbol symbol;
                    if (!this.symbols.TryGetValue(wixSimpleReferenceRow.SymbolicName, out symbol))
                    {
                        Messaging.Instance.OnMessage(WixErrors.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName));
                    }
                    else // see if the symbol (and any of its duplicates) are appropriately accessible.
                    {
                        IList<Symbol> accessible = DetermineAccessibleSymbols(section, symbol);
                        if (!accessible.Any())
                        {
                            Messaging.Instance.OnMessage(WixErrors.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName, symbol.Access));
                        }
                        else if (1 == accessible.Count)
                        {
                            Symbol accessibleSymbol = accessible[0];
                            this.referencedSymbols.Add(accessibleSymbol);

                            if (null != accessibleSymbol.Section)
                            {
                                RecursivelyResolveReferences(accessibleSymbol.Section);
                            }
                        }
                        else // display errors for the duplicate symbols.
                        {
                            Symbol accessibleSymbol = accessible[0];
                            string referencingSourceLineNumber = wixSimpleReferenceRow.SourceLineNumbers.ToString();
                            if (String.IsNullOrEmpty(referencingSourceLineNumber))
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateSymbol(accessibleSymbol.Row.SourceLineNumbers, accessibleSymbol.Name));
                            }
                            else
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateSymbol(accessibleSymbol.Row.SourceLineNumbers, accessibleSymbol.Name, referencingSourceLineNumber));
                            }

                            foreach (Symbol accessibleDuplicate in accessible.Skip(1))
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateSymbol2(accessibleDuplicate.Row.SourceLineNumbers));
                            }
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
        private IList<Symbol> DetermineAccessibleSymbols(Section referencingSection, Symbol symbol)
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
        private bool AccessibleSymbol(Section referencingSection, Symbol symbol)
        {
            switch (symbol.Access)
            {
                case AccessModifier.Public:
                    return true;
                case AccessModifier.Internal:
                    return symbol.Row.Section.IntermediateId.Equals(referencingSection.IntermediateId) || (null != symbol.Row.Section.LibraryId && symbol.Row.Section.LibraryId.Equals(referencingSection.LibraryId));
                case AccessModifier.Protected:
                    return symbol.Row.Section.IntermediateId.Equals(referencingSection.IntermediateId);
                case AccessModifier.Private:
                    return referencingSection == symbol.Section;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
