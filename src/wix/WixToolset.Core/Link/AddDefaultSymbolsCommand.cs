// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class AddDefaultSymbolsCommand
    {
        public static readonly string WixStandardInstallFolder = "INSTALLFOLDER";
        public static readonly string WixStandardInstallFolderParent = "ProgramFiles6432Folder";
        public static readonly string WixStandardInstallFolderReference = "Directory:INSTALLFOLDER";

        public AddDefaultSymbolsCommand(FindEntrySectionAndLoadSymbolsCommand find, IList<IntermediateSection> sections)
        {
            this.Find = find;
            this.Sections = sections;
        }

        public IList<IntermediateSection> Sections { get; }

        public FindEntrySectionAndLoadSymbolsCommand Find { get; }

        public void Execute()
        {
            if (this.Find.EntrySection.Type != SectionType.Package)
            {
                // Only packages...for now.
                return;
            }

            if (!this.Find.SymbolsByName.ContainsKey(WixStandardInstallFolderReference))
            {
                var sourceLineNumber = new SourceLineNumber("DefaultInstallFolder");

                this.AddSymbolsToNewSection(WixStandardInstallFolder,
                    new DirectorySymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardInstallFolder))
                    {
                        ParentDirectoryRef = WixStandardInstallFolderParent,
                        Name = "!(bind.Property.Manufacturer) !(bind.Property.ProductName)",
                        SourceName = ".",
                    },
                    new WixSimpleReferenceSymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardInstallFolder))
                    {
                        Table = "Directory",
                        PrimaryKeys = WixStandardInstallFolderParent,
                    }
                );
            }
        }

        private void AddSymbolsToNewSection(string sectionId, params IntermediateSymbol[] symbols)
        {
            var section = new IntermediateSection(sectionId, SectionType.Fragment);
            this.Sections.Add(section);

            foreach (var symbol in symbols)
            {
                section.AddSymbol(symbol);

                var symbolWithSection = new SymbolWithSection(section, symbol);
                var fullName = symbolWithSection.GetFullName();
                this.Find.SymbolsByName.Add(fullName, symbolWithSection);
            }
        }
    }
}
