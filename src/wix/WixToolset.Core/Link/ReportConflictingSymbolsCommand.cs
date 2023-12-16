// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class ReportConflictingSymbolsCommand
    {
        public ReportConflictingSymbolsCommand(IMessaging messaging, IReadOnlyCollection<SymbolWithSection> possibleConflicts, ISet<IntermediateSection> resolvedSections)
        {
            this.Messaging = messaging;
            this.PossibleConflicts = possibleConflicts;
            this.ResolvedSections = resolvedSections;
        }

        private IMessaging Messaging { get; }

        private IReadOnlyCollection<SymbolWithSection> PossibleConflicts { get; }

        private ISet<IntermediateSection> ResolvedSections { get; }

        public void Execute()
        {
            // Do a quick check if there are any possibly conflicting symbols. Hopefully the symbols with possible conflicts
            // list is a very short list (empty should be the most common).
            //
            // If we have conflicts then we'll do a more costly check to see if the possible conflicting
            // symbols are in sections we actually referenced. From the resulting set, show an error for each duplicate
            // (aka: conflicting) symbol.
            if (0 < this.PossibleConflicts.Count)
            {
                foreach (var referencedDuplicate in this.PossibleConflicts.Where(s => this.ResolvedSections.Contains(s.Section)))
                {
                    var actuallyReferencedDuplicates = referencedDuplicate.PossiblyConflicts.Where(s => this.ResolvedSections.Contains(s.Section)).ToList();

                    if (actuallyReferencedDuplicates.Count > 0)
                    {
                        var conflicts = actuallyReferencedDuplicates.Append(referencedDuplicate).ToList();
                        var virtualConflicts = conflicts.Where(s => s.Access == AccessModifier.Virtual).ToList();
                        var overrideConflicts = conflicts.Where(s => s.Access == AccessModifier.Override).ToList();
                        var otherConflicts = conflicts.Where(s => s.Access != AccessModifier.Virtual && s.Access != AccessModifier.Override).ToList();

                        IEnumerable<SymbolWithSection> reportDuplicates = actuallyReferencedDuplicates;

                        // If multiple symbols are virtual, use the duplicate virtual symbol message.
                        if (virtualConflicts.Count > 1)
                        {
                            var first = virtualConflicts[0];
                            var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                            reportDuplicates = virtualConflicts.Skip(1);

                            this.Messaging.Write(LinkerErrors.DuplicateVirtualSymbol(first.Symbol, referencingSourceLineNumber));
                        }
                        else if (virtualConflicts.Count == 1 && otherConflicts.Count > 0)
                        {
                            var first = otherConflicts[0];
                            var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                            reportDuplicates = virtualConflicts;

                            this.Messaging.Write(LinkerErrors.VirtualSymbolMustBeOverridden(first.Symbol, referencingSourceLineNumber));
                        }
                        else
                        {
                            var referencingSourceLineNumber = referencedDuplicate.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                            this.Messaging.Write(LinkerErrors.DuplicateSymbol(referencedDuplicate.Symbol, referencingSourceLineNumber));
                        }

                        foreach (var duplicate in reportDuplicates)
                        {
                            this.Messaging.Write(LinkerErrors.DuplicateSymbol2(duplicate.Symbol));
                        }
                    }
                }
            }
        }
    }
}
