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
        public IDictionary<string, TupleWithSection> TuplesByName { get; private set; }

        /// <summary>
        /// Gets the collection of possibly conflicting symbols.
        /// </summary>
        public IEnumerable<TupleWithSection> PossibleConflicts { get; private set; }

        public void Execute()
        {
            var tuplesByName = new Dictionary<string, TupleWithSection>();
            var possibleConflicts = new HashSet<TupleWithSection>();

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
                        this.Messaging.Write(ErrorMessages.MultipleEntrySections(this.EntrySection.Tuples.FirstOrDefault()?.SourceLineNumbers, this.EntrySection.Id, section.Id));
                        this.Messaging.Write(ErrorMessages.MultipleEntrySections2(section.Tuples.FirstOrDefault()?.SourceLineNumbers));
                    }
                }

                // Load all the symbols from the section's tables that create symbols.
                foreach (var tuple in section.Tuples.Where(t => t.Id != null))
                {
                    var symbol = new TupleWithSection(section, tuple);

                    if (!tuplesByName.TryGetValue(symbol.Name, out var existingSymbol))
                    {
                        tuplesByName.Add(symbol.Name, symbol);
                    }
                    else // uh-oh, duplicate symbols.
                    {
                        // If the duplicate symbols are both private directories, there is a chance that they
                        // point to identical tuples. Identical directory tuples are redundant and will not cause
                        // conflicts.
                        if (AccessModifier.Private == existingSymbol.Access && AccessModifier.Private == symbol.Access &&
                            TupleDefinitionType.Directory == existingSymbol.Tuple.Definition.Type && existingSymbol.Tuple.IsIdentical(symbol.Tuple))
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

            this.TuplesByName = tuplesByName;
            this.PossibleConflicts = possibleConflicts;
        }
    }
}
