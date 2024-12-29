// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class AddDefaultSymbolsCommand
    {
        public static readonly string WixStandardInstallFolder = "INSTALLFOLDER";
        public static readonly string WixStandardPerMachineInstallFolderParent = "ProgramFiles6432Folder";
        public static readonly string WixStandardPerUserInstallFolderParent = "PerUserProgramFilesFolder";
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
            if (this.Find.EntrySection.Type != SectionType.Package || this.Find.EntrySection.Symbols.Count == 0)
            {
                // Only packages...for now.
                return;
            }

            var packageSymbol = this.Find.EntrySection.Symbols.OfType<WixPackageSymbol>().First();

            // If a directory with id INSTALLFOLDER hasn't been authored, provide a default one.
            if (!this.Find.SymbolsByName.ContainsKey(WixStandardInstallFolderReference))
            {
                var sourceLineNumber = new SourceLineNumber("DefaultInstallFolder");
                var parentDirectoryRef = (packageSymbol.Scope == WixPackageScope.PerUser) ? WixStandardPerUserInstallFolderParent : WixStandardPerMachineInstallFolderParent;

                this.AddSymbolsToNewSection(WixStandardInstallFolder,
                    new DirectorySymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardInstallFolder))
                    {
                        ParentDirectoryRef = parentDirectoryRef,
                        Name = "!(bind.Property.Manufacturer) !(bind.Property.ProductName)",
                        SourceName = ".",
                    },
                    new WixSimpleReferenceSymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardInstallFolder))
                    {
                        Table = "Directory",
                        PrimaryKeys = parentDirectoryRef,
                    }
                );
            }

            // If an upgrade hasn't been authored and the upgrade strategy is MajorUpgrade,
            // conjure a default major upgrade with the stdlib localization string for the
            // downgrade error message.
            var symbols = this.Sections.SelectMany(section => section.Symbols);
            if (!symbols.OfType<UpgradeSymbol>().Any(us => !us.OnlyDetect))
            {
                if (packageSymbol.UpgradeStrategy == WixPackageUpgradeStrategy.MajorUpgrade && !String.IsNullOrEmpty(packageSymbol?.UpgradeCode))
                {
                    this.AddDefaultMajorUpgrade(packageSymbol);
                }
            }
        }

        private void AddDefaultMajorUpgrade(WixPackageSymbol packageSymbol)
        {
            this.AddSymbols(this.Find.EntrySection,
                new UpgradeSymbol(packageSymbol.SourceLineNumbers)
                {
                    UpgradeCode = packageSymbol.UpgradeCode,
                    MigrateFeatures = true,
                    ActionProperty = WixUpgradeConstants.UpgradeDetectedProperty,
                    VersionMax = packageSymbol.Version,
                    Language = packageSymbol.Language,
                },
                new UpgradeSymbol(packageSymbol.SourceLineNumbers)
                {
                    UpgradeCode = packageSymbol.UpgradeCode,
                    VersionMin = packageSymbol.Version,
                    Language = packageSymbol.Language,
                    OnlyDetect = true,
                    ActionProperty = WixUpgradeConstants.DowngradeDetectedProperty,
                },
                new LaunchConditionSymbol(packageSymbol.SourceLineNumbers)
                {
                    Condition = WixUpgradeConstants.DowngradePreventedCondition,
                    Description = "!(loc.WixDowngradePreventedMessage)",
                },
                new WixActionSymbol(packageSymbol.SourceLineNumbers,
                    new Identifier(AccessModifier.Global, SequenceTable.InstallExecuteSequence, "RemoveExistingProducts"))
                {
                    SequenceTable = SequenceTable.InstallExecuteSequence,
                    Action = "RemoveExistingProducts",
                    After = "InstallValidate",
                    Overridable = true,
                },
                new WixSimpleReferenceSymbol(packageSymbol.SourceLineNumbers)
                {
                    Table = SymbolDefinitions.WixAction.Name,
                    PrimaryKeys = "InstallExecuteSequence/InstallValidate",
                });
        }

        private void AddSymbolsToNewSection(string sectionId, params IntermediateSymbol[] symbols)
        {
            var section = new IntermediateSection(sectionId, SectionType.Fragment);

            this.Sections.Add(section);

            this.AddSymbols(section, symbols);
        }

        private void AddSymbols(IntermediateSection section, params IntermediateSymbol[] symbols)
        {
            foreach (var symbol in symbols)
            {
                section.AddSymbol(symbol);

                if (!String.IsNullOrEmpty(symbol.Id?.Id))
                {
                    var symbolWithSection = new SymbolWithSection(section, symbol);
                    var fullName = symbolWithSection.GetFullName();
                    if (!this.Find.SymbolsByName.ContainsKey(fullName))
                    {
                        this.Find.SymbolsByName.Add(fullName, symbolWithSection);
                    }
                }
            }
        }
    }
}
