// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    public class ReportConflictingSymbolsCommand : ICommand
    {
        private IEnumerable<Symbol> possibleConflicts;
        private IEnumerable<Section> resolvedSections;

        public ReportConflictingSymbolsCommand(IEnumerable<Symbol> possibleConflicts, IEnumerable<Section> resolvedSections)
        {
            this.possibleConflicts = possibleConflicts;
            this.resolvedSections = resolvedSections;
        }

        public void Execute()
        {
            // Do a quick check if there are any possibly conflicting symbols that don't come from tables that allow
            // overriding. Hopefully the symbols with possible conflicts list is usually very short list (empty should
            // be the most common). If we find any matches, we'll do a more costly check to see if the possible conflicting
            // symbols are in sections we actually referenced. From the resulting set, show an error for each duplicate
            // (aka: conflicting) symbol. This should catch any rows with colliding primary keys (since symbols are based
            // on the primary keys of rows).
            List<Symbol> illegalDuplicates = possibleConflicts.Where(s => "WixAction" != s.Row.Table.Name && "WixVariable" != s.Row.Table.Name).ToList();
            if (0 < illegalDuplicates.Count)
            {
                HashSet<Section> referencedSections = new HashSet<Section>(resolvedSections);
                foreach (Symbol referencedDuplicateSymbol in illegalDuplicates.Where(s => referencedSections.Contains(s.Section)))
                {
                    List<Symbol> actuallyReferencedDuplicateSymbols = referencedDuplicateSymbol.PossiblyConflictingSymbols.Where(s => referencedSections.Contains(s.Section)).ToList();

                    if (actuallyReferencedDuplicateSymbols.Any())
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicateSymbol(referencedDuplicateSymbol.Row.SourceLineNumbers, referencedDuplicateSymbol.Name));

                        foreach (Symbol duplicate in actuallyReferencedDuplicateSymbols)
                        {
                            Messaging.Instance.OnMessage(WixErrors.DuplicateSymbol2(duplicate.Row.SourceLineNumbers));
                        }
                    }
                }
            }
        }
    }
}
