// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public class ReportConflictingSymbolsCommand
    {
        public ReportConflictingSymbolsCommand(IMessaging messaging, IEnumerable<Symbol> possibleConflicts, IEnumerable<IntermediateSection> resolvedSections)
        {
            this.Messaging = messaging;
            this.PossibleConflicts = possibleConflicts;
            this.ResolvedSections = resolvedSections;
        }

        private IMessaging Messaging { get; }

        private  IEnumerable<Symbol> PossibleConflicts { get; }

        private IEnumerable<IntermediateSection> ResolvedSections { get; }

        public void Execute()
        {
            // Do a quick check if there are any possibly conflicting symbols that don't come from tables that allow
            // overriding. Hopefully the symbols with possible conflicts list is usually very short list (empty should
            // be the most common). If we find any matches, we'll do a more costly check to see if the possible conflicting
            // symbols are in sections we actually referenced. From the resulting set, show an error for each duplicate
            // (aka: conflicting) symbol. This should catch any rows with colliding primary keys (since symbols are based
            // on the primary keys of rows).
            var illegalDuplicates = this.PossibleConflicts.Where(s => s.Row.Definition.Type != TupleDefinitionType.WixAction && s.Row.Definition.Type != TupleDefinitionType.WixVariable).ToList();
            if (0 < illegalDuplicates.Count)
            {
                var referencedSections = new HashSet<IntermediateSection>(this.ResolvedSections);

                foreach (Symbol referencedDuplicateSymbol in illegalDuplicates.Where(s => referencedSections.Contains(s.Section)))
                {
                    List<Symbol> actuallyReferencedDuplicateSymbols = referencedDuplicateSymbol.PossiblyConflictingSymbols.Where(s => referencedSections.Contains(s.Section)).ToList();

                    if (actuallyReferencedDuplicateSymbols.Any())
                    {
                        this.Messaging.Write(ErrorMessages.DuplicateSymbol(referencedDuplicateSymbol.Row.SourceLineNumbers, referencedDuplicateSymbol.Name));

                        foreach (Symbol duplicate in actuallyReferencedDuplicateSymbols)
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol2(duplicate.Row.SourceLineNumbers));
                        }
                    }
                }
            }
        }
    }
}
