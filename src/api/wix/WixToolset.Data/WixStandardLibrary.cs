// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data.Bind;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// WiX Standard Library implementation.
    /// </summary>
    public static class WixStandardLibrary
    {
        private const string WixStandardLibraryId = "wixstd";

        /// <summary>
        /// Build the wixstd.wixlib Intermediate.
        /// </summary>
        /// <param name="platform">Target platform for the wixstd.wixlib</param>
        /// <returns>Intermediate containing the wixstd.wixlib.</returns>
        public static Intermediate Build(Platform platform)
        {
            var localizations = YieldLocalizations();

            var sections = YieldSections(platform);

            return new Intermediate(WixStandardLibraryId, IntermediateLevels.Combined, sections, localizations.ToDictionary(l => l.Culture, StringComparer.OrdinalIgnoreCase));
        }

        private static IEnumerable<Localization> YieldLocalizations()
        {
            var sourceLineNumber = new SourceLineNumber("wixstd.wixlib");

            var strings = new[] {
                new BindVariable()
                {
                    SourceLineNumbers = sourceLineNumber,
                    Id = "WixDowngradePreventedMessage",
                    Value = "A newer version of [ProductName] is already installed.",
                    Overridable = true,
                },
            };

            var localizedControls = new LocalizedControl[0];

            yield return new Localization(LocalizationLocation.Library, null, null, String.Empty, strings.ToDictionary(s => s.Id), localizedControls.ToDictionary(l => l.GetKey()));
        }

        private static IEnumerable<IntermediateSection> YieldSections(Platform platform)
        {
            var sourceLineNumber = new SourceLineNumber("wixstd.wixlib");

            // Actions.
            foreach (var actionSymbol in WindowsInstallerStandard.StandardActions())
            {
                var symbol = new WixActionSymbol(sourceLineNumber, new Identifier(actionSymbol.Id.Access, actionSymbol.Id.Id))
                {
                    Action = actionSymbol.Action,
                    SequenceTable = actionSymbol.SequenceTable,
                    Sequence = actionSymbol.Sequence,
                    Condition = actionSymbol.Condition,
                };

                var section = CreateSectionAroundSymbol(symbol);

                yield return section;
            }

            // Directories.
            foreach (var id in WindowsInstallerStandard.StandardDirectoryIds())
            {
                var symbol = new DirectorySymbol(sourceLineNumber, new Identifier(AccessModifier.Virtual, id))
                {
                    ParentDirectoryRef = GetStandardDirectoryParent(id, platform),
                    Name = WindowsInstallerStandard.TryGetStandardDirectoryName(id, out var name) ? name : throw new InvalidOperationException("Standard directories must have a default name")
                };

                var section = CreateSectionAroundSymbol(symbol);

                // Add a reference for the more complicated parent directory references.
                if (symbol.ParentDirectoryRef != null && symbol.ParentDirectoryRef != "TARGEDIR")
                {
                    section.AddSymbol(new WixSimpleReferenceSymbol(sourceLineNumber)
                    {
                        Table = "Directory",
                        PrimaryKeys = symbol.ParentDirectoryRef
                    });
                }

                yield return section;
            }

            // Default feature.
            {
                var symbol = new FeatureSymbol(sourceLineNumber, new Identifier(AccessModifier.Virtual, WixStandardLibraryIdentifiers.DefaultFeatureName))
                {
                    Level = 1,
                    Display = 0,
                    InstallDefault = FeatureInstallDefault.Local,
                };

                var section = CreateSectionAroundSymbol(symbol);

                yield return section;
            }

            // Package References.
            {
                var section = CreateSection(WixStandardLibraryIdentifiers.WixStandardPackageReferences);

                section.AddSymbol(new WixFragmentSymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardLibraryIdentifiers.WixStandardPackageReferences)));

                section.AddSymbol(new WixSimpleReferenceSymbol(sourceLineNumber)
                {
                    Table = SymbolDefinitions.Directory.Name,
                    PrimaryKeys = "TARGETDIR"
                });

                yield return section;
            }

            // Module References.
            {
                var section = CreateSection(WixStandardLibraryIdentifiers.WixStandardModuleReferences);

                section.AddSymbol(new WixFragmentSymbol(sourceLineNumber, new Identifier(AccessModifier.Global, WixStandardLibraryIdentifiers.WixStandardModuleReferences)));

                section.AddSymbol(new WixSimpleReferenceSymbol(sourceLineNumber)
                {
                    Table = SymbolDefinitions.Directory.Name,
                    PrimaryKeys = "TARGETDIR"
                });

                yield return section;
            }
        }

        private static IntermediateSection CreateSection(string sectionId)
        {
            return new IntermediateSection(sectionId, SectionType.Fragment, WixStandardLibraryId).AssignToLibrary(WixStandardLibraryId);
        }

        private static IntermediateSection CreateSectionAroundSymbol(IntermediateSymbol symbol)
        {
            var section = CreateSection(symbol.Id.Id);

            section.AddSymbol(symbol);

            return section;
        }

        private static string GetStandardDirectoryParent(string directoryId, Platform platform)
        {
            switch (directoryId)
            {
                case "TARGETDIR":
                    return null;

                case "CommonFiles6432Folder":
                    return platform == Platform.X86 ? "CommonFilesFolder" : "CommonFiles64Folder";

                case "ProgramFiles6432Folder":
                    return platform == Platform.X86 ? "ProgramFilesFolder" : "ProgramFiles64Folder";

                case "System6432Folder":
                    return platform == Platform.X86 ? "SystemFolder" : "System64Folder";

                default:
                    return "TARGETDIR";
            }
        }
    }
}
