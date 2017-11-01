// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    internal class FindEntrySectionAndLoadSymbolsCommand : ICommand
    {
        public FindEntrySectionAndLoadSymbolsCommand(IEnumerable<IntermediateSection> sections)
        {
            this.Sections = sections;
        }

        private IEnumerable<IntermediateSection> Sections { get; }

        /// <summary>
        /// Sets the expected entry output type, based on output file extension provided to the linker.
        /// </summary>
        public OutputType ExpectedOutputType { private get; set; }

        /// <summary>
        /// Gets the located entry section after the command is executed.
        /// </summary>
        public IntermediateSection EntrySection { get; private set; }

        /// <summary>
        /// Gets the collection of loaded symbols.
        /// </summary>
        public IDictionary<string, Symbol> Symbols { get; private set; }

        /// <summary>
        /// Gets the collection of possibly conflicting symbols.
        /// </summary>
        public IEnumerable<Symbol> PossiblyConflictingSymbols { get; private set; }

        public void Execute()
        {
            Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();
            HashSet<Symbol> possibleConflicts = new HashSet<Symbol>();

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
                    //    Messaging.Instance.OnMessage(WixWarnings.UnexpectedEntrySection(section.SourceLineNumbers, section.Type.ToString(), expectedEntrySectionType.ToString(), outputExtension));
                    //}

                    if (null == this.EntrySection)
                    {
                        this.EntrySection = section;
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.MultipleEntrySections(this.EntrySection.Tuples.FirstOrDefault()?.SourceLineNumbers, this.EntrySection.Id, section.Id));
                        Messaging.Instance.OnMessage(WixErrors.MultipleEntrySections2(section.Tuples.FirstOrDefault()?.SourceLineNumbers));
                    }
                }

                // Load all the symbols from the section's tables that create symbols.
                foreach (var row in section.Tuples.Where(t => t.Id != null))
                {
                    var symbol = new Symbol(section, row);

                    if (!symbols.TryGetValue(symbol.Name, out var existingSymbol))
                    {
                        symbols.Add(symbol.Name, symbol);
                    }
                    else // uh-oh, duplicate symbols.
                    {
                        // If the duplicate symbols are both private directories, there is a chance that they
                        // point to identical tuples. Identical directory tuples are redundant and will not cause
                        // conflicts.
                        if (AccessModifier.Private == existingSymbol.Access && AccessModifier.Private == symbol.Access &&
                            TupleDefinitionType.Directory == existingSymbol.Row.Definition.Type && existingSymbol.Row.IsIdentical(symbol.Row))
                        {
                            // Ensure identical symbol's tuple is marked redundant to ensure (should the tuple be
                            // referenced into the final output) it will not add duplicate primary keys during
                            // the .IDT importing.
                            //symbol.Row.Redundant = true; - TODO: remove this
                            existingSymbol.AddRedundant(symbol);
                        }
                        else
                        {
                            existingSymbol.AddPossibleConflict(symbol);
                            possibleConflicts.Add(existingSymbol);
                        }
                    }
                }
            }

            this.Symbols = symbols;
            this.PossiblyConflictingSymbols = possibleConflicts;
        }
    }
}
