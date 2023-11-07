// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class ReportConflictingSymbolsCommand
    {
        public ReportConflictingSymbolsCommand(IMessaging messaging, IEnumerable<SymbolWithSection> possibleConflicts, IEnumerable<IntermediateSection> resolvedSections)
        {
            this.Messaging = messaging;
            this.PossibleConflicts = possibleConflicts;
            this.ResolvedSections = resolvedSections;
        }

        private IMessaging Messaging { get; }

        private  IEnumerable<SymbolWithSection> PossibleConflicts { get; }

        private IEnumerable<IntermediateSection> ResolvedSections { get; }

        public void Execute()
        {
            // Do a quick check if there are any possibly conflicting symbols that don't come from tables that allow
            // overriding. Hopefully the symbols with possible conflicts list is usually very short list (empty should
            // be the most common). If we find any matches, we'll do a more costly check to see if the possible conflicting
            // symbols are in sections we actually referenced. From the resulting set, show an error for each duplicate
            // (aka: conflicting) symbol.
            var illegalDuplicates = this.PossibleConflicts.Where(s => s.Symbol.Definition.Type != SymbolDefinitionType.WixAction && s.Symbol.Definition.Type != SymbolDefinitionType.WixVariable).ToList();
            if (0 < illegalDuplicates.Count)
            {
                var referencedSections = new HashSet<IntermediateSection>(this.ResolvedSections);

                foreach (var referencedDuplicate in illegalDuplicates.Where(s => referencedSections.Contains(s.Section)))
                {
                    var actuallyReferencedDuplicates = referencedDuplicate.PossiblyConflicts.Where(s => referencedSections.Contains(s.Section)).ToList();

                    if (actuallyReferencedDuplicates.Any())
                    {
                        var fullName = referencedDuplicate.GetFullName();

                        this.Messaging.Write(ErrorMessages.DuplicateSymbol(referencedDuplicate.Symbol.SourceLineNumbers, fullName));

                        foreach (var duplicate in actuallyReferencedDuplicates)
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol2(duplicate.Symbol.SourceLineNumbers));
                        }
                    }
                }
            }
        }
    }
}
