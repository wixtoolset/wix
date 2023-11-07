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
        public ISet<IntermediateSymbol> IdenticalDirectorySymbols { get; private set; }

        public void Execute()
        {
            var symbolsByName = new Dictionary<string, SymbolWithSection>();
            var possibleConflicts = new HashSet<SymbolWithSection>();
            var identicalDirectorySymbols = new HashSet<IntermediateSymbol>();

            if (!Enum.TryParse(this.ExpectedOutputType.ToString(), out SectionType expectedEntrySectionType))
            {
                expectedEntrySectionType = SectionType.Unknown;
            }

            foreach (var section in this.Sections)
            {
                // Try to find the one and only entry section.
                if (SectionType.Package == section.Type || SectionType.Module == section.Type || SectionType.PatchCreation == section.Type || SectionType.Patch == section.Type || SectionType.Bundle == section.Type)
                {
                    if (SectionType.Unknown != expectedEntrySectionType && section.Type != expectedEntrySectionType)
                    {
                        this.Messaging.Write(WarningMessages.UnexpectedEntrySection(section.Symbols.FirstOrDefault()?.SourceLineNumbers, section.Type.ToString(), expectedEntrySectionType.ToString()));
                    }

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

                // Load all the symbols from the section's tables that can be referenced (i.e. have an Id).
                foreach (var symbol in section.Symbols.Where(t => t.Id != null))
                {
                    var symbolWithSection = new SymbolWithSection(section, symbol);
                    var fullName = symbolWithSection.GetFullName();

                    if (!symbolsByName.TryGetValue(fullName, out var existingSymbol))
                    {
                        symbolsByName.Add(fullName, symbolWithSection);
                    }
                    else // uh-oh, duplicate symbols.
                    {
                        // If the duplicate symbols are both private directories, there is a chance that they
                        // point to identical symbols. Identical directory symbols should be treated as redundant.
                        // and not cause conflicts.
                        if (AccessModifier.Section == existingSymbol.Access && AccessModifier.Section == symbolWithSection.Access &&
                            SymbolDefinitionType.Directory == existingSymbol.Symbol.Definition.Type && existingSymbol.Symbol.IsIdentical(symbolWithSection.Symbol))
                        {
                            // Ensure identical symbols are tracked to ensure that only one symbol will end up in linked intermediate.
                            identicalDirectorySymbols.Add(existingSymbol.Symbol);
                            identicalDirectorySymbols.Add(symbolWithSection.Symbol);
                        }
                        else
                        {
                            symbolWithSection.AddPossibleConflict(existingSymbol);
                            existingSymbol.AddPossibleConflict(symbolWithSection);
                            possibleConflicts.Add(symbolWithSection);
                        }
                    }
                }
            }

            this.SymbolsByName = symbolsByName;
            this.PossibleConflicts = possibleConflicts;
            this.IdenticalDirectorySymbols = identicalDirectorySymbols;
        }
    }
}
