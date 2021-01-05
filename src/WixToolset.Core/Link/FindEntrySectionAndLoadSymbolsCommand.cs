// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class FindEntrySectionAndLoadSymbolsCommand
    {
        public FindEntrySectionAndLoadSymbolsCommand(IMessaging messaging, IEnumerable<IntermediateSection> sections, OutputType expectedOutpuType)
        {
            this.Messaging = messaging;
            this.Sections = sections;
            this.ExpectedOutputType = expectedOutpuType;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<IntermediateSection> Sections { get; }

        private OutputType ExpectedOutputType { get; }

        /// <summary>
        /// Gets the located entry section after the command is executed.
        /// </summary>
        public IntermediateSection EntrySection { get; private set; }

        /// <summary>
        /// Gets the collection of loaded symbols.
        /// </summary>
        public IDictionary<string, SymbolWithSection> SymbolsByName { get; private set; }

        /// <summary>
        /// Gets the collection of possibly conflicting symbols.
        /// </summary>
        public IEnumerable<SymbolWithSection> PossibleConflicts { get; private set; }

        /// <summary>
        /// Gets the collection of redundant symbols that should not be included
        /// in the final output.
        /// </summary>
        public ISet<IntermediateSymbol> RedundantSymbols { get; private set; }

        public void Execute()
        {
            var symbolsByName = new Dictionary<string, SymbolWithSection>();
            var possibleConflicts = new HashSet<SymbolWithSection>();
            var redundantSymbols = new HashSet<IntermediateSymbol>();

            if (!Enum.TryParse(this.ExpectedOutputType.ToString(), out SectionType expectedEntrySectionType))
            {
                expectedEntrySectionType = SectionType.Unknown;
            }

            foreach (var section in this.Sections)
            {
                // Try to find the one and only entry section.
                if (SectionType.Product == section.Type || SectionType.Module == section.Type || SectionType.PatchCreation == section.Type || SectionType.Patch == section.Type || SectionType.Bundle == section.Type)
                {
                    // TODO: remove this?
                    //if (SectionType.Unknown != expectedEntrySectionType && section.Type != expectedEntrySectionType)
                    //{
                    //    string outputExtension = Output.GetExtension(this.ExpectedOutputType);
                    //    this.Messaging.Write(WixWarnings.UnexpectedEntrySection(section.SourceLineNumbers, section.Type.ToString(), expectedEntrySectionType.ToString(), outputExtension));
                    //}

                    if (null == this.EntrySection)
                    {
                        this.EntrySection = section;
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.MultipleEntrySections(this.EntrySection.Symbols.FirstOrDefault()?.SourceLineNumbers, this.EntrySection.Id, section.Id));
                        this.Messaging.Write(ErrorMessages.MultipleEntrySections2(section.Symbols.FirstOrDefault()?.SourceLineNumbers));
                    }
                }

                // Load all the symbols from the section's tables that create symbols.
                foreach (var symbol in section.Symbols.Where(t => t.Id != null))
                {
                    var symbolWithSection = new SymbolWithSection(section, symbol);

                    if (!symbolsByName.TryGetValue(symbolWithSection.Name, out var existingSymbol))
                    {
                        symbolsByName.Add(symbolWithSection.Name, symbolWithSection);
                    }
                    else // uh-oh, duplicate symbols.
                    {
                        // If the duplicate symbols are both private directories, there is a chance that they
                        // point to identical symbols. Identical directory symbols are redundant and will not cause
                        // conflicts.
                        if (AccessModifier.Private == existingSymbol.Access && AccessModifier.Private == symbolWithSection.Access &&
                            SymbolDefinitionType.Directory == existingSymbol.Symbol.Definition.Type && existingSymbol.Symbol.IsIdentical(symbolWithSection.Symbol))
                        {
                            // Ensure identical symbol's symbol is marked redundant to ensure (should the symbol be
                            // referenced into the final output) it will not add duplicate primary keys during
                            // the .IDT importing.
                            existingSymbol.AddRedundant(symbolWithSection);
                            redundantSymbols.Add(symbolWithSection.Symbol);
                        }
                        else
                        {
                            existingSymbol.AddPossibleConflict(symbolWithSection);
                            possibleConflicts.Add(symbolWithSection);
                        }
                    }
                }
            }

            this.SymbolsByName = symbolsByName;
            this.PossibleConflicts = possibleConflicts;
            this.RedundantSymbols = redundantSymbols;
        }
    }
}
