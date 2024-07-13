// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class ProcessConflictingSymbolsCommand
    {
        public ProcessConflictingSymbolsCommand(IMessaging messaging, IReadOnlyCollection<SymbolWithSection> possibleConflicts, IReadOnlyCollection<SymbolWithSection> overrideSymbols, ISet<IntermediateSection> resolvedSections)
        {
            this.Messaging = messaging;
            this.PossibleConflicts = possibleConflicts;
            this.OverrideSymbols = overrideSymbols;
            this.ResolvedSections = resolvedSections;
        }

        private IMessaging Messaging { get; }

        private IReadOnlyCollection<SymbolWithSection> PossibleConflicts { get; }

        private ISet<IntermediateSection> ResolvedSections { get; }

        private IReadOnlyCollection<SymbolWithSection> OverrideSymbols { get; }

        /// <summary>
        /// Gets the collection of overridden symbols that should not be included
        /// in the final output.
        /// </summary>
        public ISet<IntermediateSymbol> OverriddenSymbols { get; private set; }

        public void Execute()
        {
            var overriddenSymbols = new HashSet<IntermediateSymbol>();

            foreach (var symbolWithConflicts in this.PossibleConflicts)
            {
                var conflicts = YieldReferencedConflicts(symbolWithConflicts, this.ResolvedSections).ToList();

                if (conflicts.Count > 1)
                {
                    IEnumerable<SymbolWithSection> reportDuplicates;

                    var virtualConflicts = conflicts.Where(s => s.Access == AccessModifier.Virtual).ToList();

                    // No virtual symbols, just plain old duplicate errors. This is the easy case.
                    if (virtualConflicts.Count == 0)
                    {
                        var first = conflicts[0];
                        reportDuplicates = conflicts.Skip(1);

                        var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                        this.Messaging.Write(LinkerErrors.DuplicateSymbol(first.Symbol, referencingSourceLineNumber));
                    }
                    else // there are virtual symbols, which complicates conflict resolution and may not be an error at all.
                    {
                        var firstVirtualSymbol = virtualConflicts[0];
                        var overrideConflicts = conflicts.Where(s => s.Access == AccessModifier.Override).ToList();

                        // If there is a single virtual symbol, there may be a single override symbol to make this a success case.
                        // All other scenarios are errors.
                        if (virtualConflicts.Count == 1)
                        {
                            var otherConflicts = conflicts.Where(s => s.Access != AccessModifier.Virtual && s.Access != AccessModifier.Override).ToList();

                            if (otherConflicts.Count > 0)
                            {
                                var first = otherConflicts[0];
                                var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                                reportDuplicates = virtualConflicts;

                                switch (first.Symbol)
                                {
                                    case WixActionSymbol action:
                                        this.Messaging.Write(LinkerErrors.VirtualSymbolMustBeOverridden(action));
                                        break;
                                    default:
                                        this.Messaging.Write(LinkerErrors.VirtualSymbolMustBeOverridden(first.Symbol, referencingSourceLineNumber));
                                        break;
                                }
                            }
                            else if (overrideConflicts.Count > 1) // multiple overrides report as normal duplicates.
                            {
                                var first = overrideConflicts[0];
                                var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                                reportDuplicates = overrideConflicts.Skip(1);

                                this.Messaging.Write(LinkerErrors.DuplicateSymbol(first.Symbol, referencingSourceLineNumber));
                            }
                            else // the single virtual symbol is overridden by a single override symbol. This is a success case.
                            {
                                var overrideSymbol = overrideConflicts[0];

                                overriddenSymbols.Add(firstVirtualSymbol.Symbol);

                                reportDuplicates = Enumerable.Empty<SymbolWithSection>();
                            }
                        }
                        else // multiple symbols are virtual, use the duplicate virtual symbol message.
                        {
                            var first = virtualConflicts[0];
                            var referencingSourceLineNumber = first.DirectReferences.FirstOrDefault()?.SourceLineNumbers;

                            reportDuplicates = virtualConflicts.Skip(1);

                            this.Messaging.Write(LinkerErrors.DuplicateVirtualSymbol(first.Symbol, referencingSourceLineNumber));
                        }

                        // Always point the override symbols at the first virtual symbol to prevent error being reported about missing overrides.
                        // There may have been errors reported above, but there was at least one virtual symbol to satisfy the overrides so we
                        // don't want extra errors in this case.
                        foreach (var overrideSymbol in overrideConflicts)
                        {
                            overrideSymbol.OverrideVirtualSymbol(firstVirtualSymbol);
                        }
                    }

                    foreach (var duplicate in reportDuplicates)
                    {
                        this.Messaging.Write(LinkerErrors.DuplicateSymbol2(duplicate.Symbol));
                    }
                }
            }

            // Ensure referenced override symbols actually overrode a virtual symbol.
            foreach (var referencedOverrideSymbol in this.OverrideSymbols.Where(s => this.ResolvedSections.Contains(s.Section)))
            {
                // The easiest check is to see if the symbol overrode a virtual symbol. If not, check to see if there were any possible
                // virtual symbols that could have been overridden. If not, then we have an error.
                if (referencedOverrideSymbol.Overrides is null)
                {
                    var otherVirtualsCount = referencedOverrideSymbol.PossiblyConflicts.Count(s => s.Access == AccessModifier.Virtual);

                    if (otherVirtualsCount == 0)
                    {
                        this.Messaging.Write(LinkerErrors.VirtualSymbolNotFoundForOverride(referencedOverrideSymbol.Symbol));
                    }
                }
            }

            this.OverriddenSymbols = overriddenSymbols;
        }

        private static IEnumerable<SymbolWithSection> YieldReferencedConflicts(SymbolWithSection symbolWithConflicts, ISet<IntermediateSection> resolvedSections)
        {
            if (resolvedSections.Contains(symbolWithConflicts.Section))
            {
                yield return symbolWithConflicts;
            }

            foreach (var possibleConflict in symbolWithConflicts.PossiblyConflicts)
            {
                if (resolvedSections.Contains(possibleConflict.Section))
                {
                    yield return possibleConflict;
                }
            }
        }
    }
}
