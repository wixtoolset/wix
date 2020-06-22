// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Core.Link;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Linker core of the WiX toolset.
    /// </summary>
    internal class Linker : ILinker
    {
        private static readonly string EmptyGuid = Guid.Empty.ToString("B");

        private readonly bool sectionIdOnRows;

        /// <summary>
        /// Creates a linker.
        /// </summary>
        internal Linker(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
            this.sectionIdOnRows = true; // TODO: what is the correct value for this?
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private ILinkContext Context { get; set; }

        /// <summary>
        /// Gets or sets the path to output unreferenced symbols to. If null or empty, there is no output.
        /// </summary>
        /// <value>The path to output the xml file.</value>
        public string UnreferencedSymbolsFile { get; set; }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages { get; set; }

        /// <summary>
        /// Links a collection of sections into an output.
        /// </summary>
        /// <returns>Output intermediate from the linking.</returns>
        public Intermediate Link(ILinkContext context)
        {
            this.Context = context;

            if (this.Context.TupleDefinitionCreator == null)
            {
                this.Context.TupleDefinitionCreator = this.ServiceProvider.GetService<ITupleDefinitionCreator>();
            }

            foreach (var extension in this.Context.Extensions)
            {
                extension.PreLink(this.Context);
            }

            var invalidIntermediates = this.Context.Intermediates.Where(i => !i.HasLevel(Data.IntermediateLevels.Compiled));
            if (invalidIntermediates.Any())
            {
                this.Messaging.Write(ErrorMessages.IntermediatesMustBeCompiled(String.Join(", ", invalidIntermediates.Select(i => i.Id))));
            }

            Intermediate intermediate = null;
            try
            {
                var sections = this.Context.Intermediates.SelectMany(i => i.Sections).ToList();
                var localizations = this.Context.Intermediates.SelectMany(i => i.Localizations).ToList();

                // Add sections from the extensions with data.
                foreach (var data in this.Context.ExtensionData)
                {
                    var library = data.GetLibrary(this.Context.TupleDefinitionCreator);

                    if (library != null)
                    {
                        sections.AddRange(library.Sections);
                    }
                }

#if MOVE_TO_BACKEND
                bool containsModuleSubstitution = false;
                bool containsModuleConfiguration = false;
#endif

                //this.activeOutput = null;

#if MOVE_TO_BACKEND
                StringCollection generatedShortFileNameIdentifiers = new StringCollection();
                Hashtable generatedShortFileNames = new Hashtable();
#endif

                var multipleFeatureComponents = new Hashtable();

                var wixVariables = new Dictionary<string, WixVariableTuple>();

#if MOVE_TO_BACKEND
                // verify that modularization types match for foreign key relationships
                foreach (TableDefinition tableDefinition in this.tableDefinitions)
                {
                    foreach (ColumnDefinition columnDefinition in tableDefinition.Columns)
                    {
                        if (null != columnDefinition.KeyTable && 0 > columnDefinition.KeyTable.IndexOf(';') && columnDefinition.IsKeyColumnSet)
                        {
                            try
                            {
                                TableDefinition keyTableDefinition = this.tableDefinitions[columnDefinition.KeyTable];

                                if (0 >= columnDefinition.KeyColumn || keyTableDefinition.Columns.Count < columnDefinition.KeyColumn)
                                {
                                    this.Messaging.Write(WixErrors.InvalidKeyColumn(tableDefinition.Name, columnDefinition.Name, columnDefinition.KeyTable, columnDefinition.KeyColumn));
                                }
                                else if (keyTableDefinition.Columns[columnDefinition.KeyColumn - 1].ModularizeType != columnDefinition.ModularizeType && ColumnModularizeType.CompanionFile != columnDefinition.ModularizeType)
                                {
                                    this.Messaging.Write(WixErrors.CollidingModularizationTypes(tableDefinition.Name, columnDefinition.Name, columnDefinition.KeyTable, columnDefinition.KeyColumn, columnDefinition.ModularizeType.ToString(), keyTableDefinition.Columns[columnDefinition.KeyColumn - 1].ModularizeType.ToString()));
                                }
                            }
                            catch (WixMissingTableDefinitionException)
                            {
                                // ignore missing table definitions - this error is caught in other places
                            }
                        }
                    }
                }
#endif

                // First find the entry section and while processing all sections load all the symbols from all of the sections.
                var find = new FindEntrySectionAndLoadSymbolsCommand(this.Messaging, sections, this.Context.ExpectedOutputType);
                find.Execute();

                // Must have found the entry section by now.
                if (null == find.EntrySection)
                {
                    throw new WixException(ErrorMessages.MissingEntrySection(this.Context.ExpectedOutputType.ToString()));
                }

                // Add the missing standard action symbols.
                this.LoadStandardActionSymbols(find.EntrySection, find.Symbols);

                // Resolve the symbol references to find the set of sections we care about for linking.
                // Of course, we start with the entry section (that's how it got its name after all).
                var resolve = new ResolveReferencesCommand(this.Messaging, find.EntrySection, find.Symbols);

                resolve.Execute();

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // Reset the sections to only those that were resolved then flatten the complex
                // references that particpate in groups.
                sections = resolve.ResolvedSections.ToList();

                // TODO: consider filtering "localizations" down to only those localizations from
                //       intermediates in the sections.

                this.FlattenSectionsComplexReferences(sections);

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // The hard part in linking is processing the complex references.
                var referencedComponents = new HashSet<string>();
                var componentsToFeatures = new ConnectToFeatureCollection();
                var featuresToFeatures = new ConnectToFeatureCollection();
                var modulesToFeatures = new ConnectToFeatureCollection();
                this.ProcessComplexReferences(find.EntrySection, sections, referencedComponents, componentsToFeatures, featuresToFeatures, modulesToFeatures);

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // Display an error message for Components that were not referenced by a Feature.
                foreach (var symbol in resolve.ReferencedSymbols.Where(s => s.Row.Definition.Type == TupleDefinitionType.Component))
                {
                    if (!referencedComponents.Contains(symbol.Name))
                    {
                        this.Messaging.Write(ErrorMessages.OrphanedComponent(symbol.Row.SourceLineNumbers, symbol.Row.Id.Id));
                    }
                }

                // Report duplicates that would ultimately end up being primary key collisions.
                var reportDupes = new ReportConflictingSymbolsCommand(this.Messaging, find.PossiblyConflictingSymbols, resolve.ResolvedSections);
                reportDupes.Execute();

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // resolve the feature to feature connects
                this.ResolveFeatureToFeatureConnects(featuresToFeatures, find.Symbols);

                // Create the section to hold the linked content.
                var resolvedSection = new IntermediateSection(find.EntrySection.Id, find.EntrySection.Type, find.EntrySection.Codepage);

                var sectionCount = 0;

                foreach (var section in sections)
                {
                    sectionCount++;

                    var sectionId = section.Id;
                    if (null == sectionId && this.sectionIdOnRows)
                    {
                        sectionId = "wix.section." + sectionCount.ToString(CultureInfo.InvariantCulture);
                    }

                    foreach (var tuple in section.Tuples)
                    {
                        var copyTuple = true; // by default, copy tuples.

                        // handle special tables
                        switch (tuple.Definition.Type)
                        {
                            case TupleDefinitionType.Class:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)ClassTupleFields.ComponentRef, (int)ClassTupleFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

#if MOVE_TO_BACKEND
                            case "CustomAction":
                                if (OutputType.Module == this.activeOutput.Type)
                                {
                                    this.activeOutput.EnsureTable(this.tableDefinitions["AdminExecuteSequence"]);
                                    this.activeOutput.EnsureTable(this.tableDefinitions["AdminUISequence"]);
                                    this.activeOutput.EnsureTable(this.tableDefinitions["AdvtExecuteSequence"]);
                                    this.activeOutput.EnsureTable(this.tableDefinitions["InstallExecuteSequence"]);
                                    this.activeOutput.EnsureTable(this.tableDefinitions["InstallUISequence"]);
                                }
                                break;

                            case "Directory":
                                foreach (Row row in table.Rows)
                                {
                                    if (OutputType.Module == this.activeOutput.Type)
                                    {
                                        string directory = row[0].ToString();
                                        if (WindowsInstallerStandard.IsStandardDirectory(directory))
                                        {
                                            // if the directory table contains references to standard windows folders
                                            // mergemod.dll will add customactions to set the MSM directory to
                                            // the same directory as the standard windows folder and will add references to
                                            // custom action to all the standard sequence tables. A problem will occur
                                            // if the MSI does not have these tables as mergemod.dll does not add these
                                            // tables to the MSI if absent. This code adds the tables in case mergemod.dll
                                            // needs them.
                                            this.activeOutput.EnsureTable(this.tableDefinitions["CustomAction"]);
                                            this.activeOutput.EnsureTable(this.tableDefinitions["AdminExecuteSequence"]);
                                            this.activeOutput.EnsureTable(this.tableDefinitions["AdminUISequence"]);
                                            this.activeOutput.EnsureTable(this.tableDefinitions["AdvtExecuteSequence"]);
                                            this.activeOutput.EnsureTable(this.tableDefinitions["InstallExecuteSequence"]);
                                            this.activeOutput.EnsureTable(this.tableDefinitions["InstallUISequence"]);
                                        }
                                        else
                                        {
                                            foreach (string standardDirectory in WindowsInstallerStandard.GetStandardDirectories())
                                            {
                                                if (directory.StartsWith(standardDirectory, StringComparison.Ordinal))
                                                {
                                                    this.Messaging.Write(WixWarnings.StandardDirectoryConflictInMergeModule(row.SourceLineNumbers, directory, standardDirectory));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
#endif
                            case TupleDefinitionType.Extension:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)ExtensionTupleFields.ComponentRef, (int)ExtensionTupleFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

#if MOVE_TO_BACKEND
                            case TupleDefinitionType.ModuleSubstitution:
                                containsModuleSubstitution = true;
                                break;

                            case TupleDefinitionType.ModuleConfiguration:
                                containsModuleConfiguration = true;
                                break;
#endif

                            case TupleDefinitionType.Assembly:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)AssemblyTupleFields.ComponentRef, (int)AssemblyTupleFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case TupleDefinitionType.PublishComponent:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)PublishComponentTupleFields.ComponentRef, (int)PublishComponentTupleFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case TupleDefinitionType.Shortcut:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)ShortcutTupleFields.ComponentRef, (int)ShortcutTupleFields.Target, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case TupleDefinitionType.TypeLib:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, (int)TypeLibTupleFields.ComponentRef, (int)TypeLibTupleFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

#if MOVE_TO_BACKEND
                            case "WixFile":
                                foreach (Row row in table.Rows)
                                {
                                    // DiskId is not valid when creating a module, so set it to
                                    // 0 for all files to ensure proper sorting in the binder
                                    if (SectionType.Module == entrySection.Type)
                                    {
                                        row[5] = 0;
                                    }

                                    // if the short file name was generated, check for collisions
                                    if (0x1 == (int)row[9])
                                    {
                                        generatedShortFileNameIdentifiers.Add((string)row[0]);
                                    }
                                }
                                break;
#endif

                            case TupleDefinitionType.WixMerge:
                                if (SectionType.Product == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(tuple, -1, (int)WixMergeTupleFields.FeatureRef, modulesToFeatures, null);
                                }
                                break;

                            case TupleDefinitionType.WixComplexReference:
                                copyTuple = false;
                                break;

                            case TupleDefinitionType.WixSimpleReference:
                                copyTuple = false;
                                break;

                            case TupleDefinitionType.WixVariable:
                            // check for colliding values and collect the wix variable rows
                            {
                                var wixVariableTuple = (WixVariableTuple)tuple;
                                var id = wixVariableTuple.Id.Id;

                                if (wixVariables.TryGetValue(id, out var collidingTuple))
                                {
                                    if (collidingTuple.Overridable && !wixVariableTuple.Overridable)
                                    {
                                        wixVariables[id] = wixVariableTuple;
                                    }
                                    else if (!wixVariableTuple.Overridable || (collidingTuple.Overridable && wixVariableTuple.Overridable))
                                    {
                                        this.Messaging.Write(ErrorMessages.WixVariableCollision(wixVariableTuple.SourceLineNumbers, id));
                                    }
                                }
                                else
                                {
                                    wixVariables.Add(id, wixVariableTuple);
                                }
                            }

                            copyTuple = false;
                            break;
                        }

                        if (copyTuple)
                        {
                            resolvedSection.AddTuple(tuple);
                        }
                    }
                }

                // copy the module to feature connections into the output
                foreach (ConnectToFeature connectToFeature in modulesToFeatures)
                {
                    foreach (var feature in connectToFeature.ConnectFeatures)
                    {
                        resolvedSection.AddTuple(new WixFeatureModulesTuple
                        {
                            FeatureRef = feature,
                            WixMergeRef = connectToFeature.ChildId
                        });
                    }
                }

#if MOVE_TO_BACKEND
                // check for missing table and add them or display an error as appropriate
                switch (this.activeOutput.Type)
                {
                    case OutputType.Module:
                        this.activeOutput.EnsureTable(this.tableDefinitions["Component"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["Directory"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["FeatureComponents"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["File"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["ModuleComponents"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["ModuleSignature"]);
                        break;
                    case OutputType.PatchCreation:
                        Table imageFamiliesTable = this.activeOutput.Tables["ImageFamilies"];
                        Table targetImagesTable = this.activeOutput.Tables["TargetImages"];
                        Table upgradedImagesTable = this.activeOutput.Tables["UpgradedImages"];

                        if (null == imageFamiliesTable || 1 > imageFamiliesTable.Rows.Count)
                        {
                            this.Messaging.Write(WixErrors.ExpectedRowInPatchCreationPackage("ImageFamilies"));
                        }

                        if (null == targetImagesTable || 1 > targetImagesTable.Rows.Count)
                        {
                            this.Messaging.Write(WixErrors.ExpectedRowInPatchCreationPackage("TargetImages"));
                        }

                        if (null == upgradedImagesTable || 1 > upgradedImagesTable.Rows.Count)
                        {
                            this.Messaging.Write(WixErrors.ExpectedRowInPatchCreationPackage("UpgradedImages"));
                        }

                        this.activeOutput.EnsureTable(this.tableDefinitions["Properties"]);
                        break;
                    case OutputType.Product:
                        this.activeOutput.EnsureTable(this.tableDefinitions["File"]);
                        this.activeOutput.EnsureTable(this.tableDefinitions["Media"]);
                        break;
                }

                this.CheckForIllegalTables(this.activeOutput);
#endif

                //correct the section Id in FeatureComponents table
                if (this.sectionIdOnRows)
                {
                    //var componentSectionIds = new Dictionary<string, string>();

                    //foreach (var componentTuple in entrySection.Tuples.OfType<ComponentTuple>())
                    //{
                    //    componentSectionIds.Add(componentTuple.Id.Id, componentTuple.SectionId);
                    //}

                    //foreach (var featureComponentTuple in entrySection.Tuples.OfType<FeatureComponentsTuple>())
                    //{
                    //    if (componentSectionIds.TryGetValue(featureComponentTuple.Component_, out var componentSectionId))
                    //    {
                    //        featureComponentTuple.SectionId = componentSectionId;
                    //    }
                    //}
                }

#if MOVE_TO_BACKEND
                // add the ModuleSubstitution table to the ModuleIgnoreTable
                if (containsModuleSubstitution)
                {
                    Table moduleIgnoreTableTable = this.activeOutput.EnsureTable(this.tableDefinitions["ModuleIgnoreTable"]);

                    Row moduleIgnoreTableRow = moduleIgnoreTableTable.CreateRow(null);
                    moduleIgnoreTableRow[0] = "ModuleSubstitution";
                }

                // add the ModuleConfiguration table to the ModuleIgnoreTable
                if (containsModuleConfiguration)
                {
                    Table moduleIgnoreTableTable = this.activeOutput.EnsureTable(this.tableDefinitions["ModuleIgnoreTable"]);

                    Row moduleIgnoreTableRow = moduleIgnoreTableTable.CreateRow(null);
                    moduleIgnoreTableRow[0] = "ModuleConfiguration";
                }
#endif

#if MOVE_TO_BACKEND
                // index all the file rows
                Table fileTable = this.activeOutput.Tables["File"];
                RowDictionary<FileRow> indexedFileRows = (null == fileTable) ? new RowDictionary<FileRow>() : new RowDictionary<FileRow>(fileTable);

                // flag all the generated short file name collisions
                foreach (string fileId in generatedShortFileNameIdentifiers)
                {
                    FileRow fileRow = indexedFileRows[fileId];

                    string[] names = fileRow.FileName.Split('|');
                    string shortFileName = names[0];

                    // create lists of conflicting generated short file names
                    if (!generatedShortFileNames.Contains(shortFileName))
                    {
                        generatedShortFileNames.Add(shortFileName, new ArrayList());
                    }
                    ((ArrayList)generatedShortFileNames[shortFileName]).Add(fileRow);
                }

                // check for generated short file name collisions
                foreach (DictionaryEntry entry in generatedShortFileNames)
                {
                    string shortFileName = (string)entry.Key;
                    ArrayList fileRows = (ArrayList)entry.Value;

                    if (1 < fileRows.Count)
                    {
                        // sort the rows by DiskId
                        fileRows.Sort();

                        this.Messaging.Write(WixWarnings.GeneratedShortFileNameConflict(((FileRow)fileRows[0]).SourceLineNumbers, shortFileName));

                        for (int i = 1; i < fileRows.Count; i++)
                        {
                            FileRow fileRow = (FileRow)fileRows[i];

                            if (null != fileRow.SourceLineNumbers)
                            {
                                this.Messaging.Write(WixWarnings.GeneratedShortFileNameConflict2(fileRow.SourceLineNumbers));
                            }
                        }
                    }
                }
#endif

                // copy the wix variable rows to the output after all overriding has been accounted for.
                foreach (var tuple in wixVariables.Values)
                {
                    resolvedSection.AddTuple(tuple);
                }

                // Bundles have groups of data that must be flattened in a way different from other types.
                this.FlattenBundleTables(resolvedSection);

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                var collate = new CollateLocalizationsCommand(this.Messaging, localizations);
                var localizationsByCulture = collate.Execute();

                intermediate = new Intermediate(resolvedSection.Id, Data.IntermediateLevels.Linked, new[] { resolvedSection }, localizationsByCulture);

#if MOVE_TO_BACKEND
                this.CheckOutputConsistency(output);
#endif
            }
            finally
            {
                foreach (var extension in this.Context.Extensions)
                {
                    extension.PostLink(intermediate);
                }
            }

            return this.Messaging.EncounteredError ? null : intermediate;
        }

#if MOVE_TO_BACKEND
        /// <summary>
        /// Checks for any tables in the output which are not allowed in the output type.
        /// </summary>
        /// <param name="output">The output to check.</param>
        private void CheckForIllegalTables(Output output)
        {
            foreach (Table table in output.Tables)
            {
                switch (output.Type)
                {
                    case OutputType.Module:
                        if ("BBControl" == table.Name ||
                            "Billboard" == table.Name ||
                            "CCPSearch" == table.Name ||
                            "Feature" == table.Name ||
                            "LaunchCondition" == table.Name ||
                            "Media" == table.Name ||
                            "Patch" == table.Name ||
                            "Upgrade" == table.Name ||
                            "WixMerge" == table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(WixErrors.UnexpectedTableInMergeModule(row.SourceLineNumbers, table.Name));
                            }
                        }
                        else if ("Error" == table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(WixWarnings.DangerousTableInMergeModule(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;
                    case OutputType.PatchCreation:
                        if (!table.Definition.Unreal &&
                            "_SummaryInformation" != table.Name &&
                            "ExternalFiles" != table.Name &&
                            "FamilyFileRanges" != table.Name &&
                            "ImageFamilies" != table.Name &&
                            "PatchMetadata" != table.Name &&
                            "PatchSequence" != table.Name &&
                            "Properties" != table.Name &&
                            "TargetFiles_OptionalData" != table.Name &&
                            "TargetImages" != table.Name &&
                            "UpgradedFiles_OptionalData" != table.Name &&
                            "UpgradedFilesToIgnore" != table.Name &&
                            "UpgradedImages" != table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(WixErrors.UnexpectedTableInPatchCreationPackage(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;
                    case OutputType.Patch:
                        if (!table.Definition.Unreal &&
                            "_SummaryInformation" != table.Name &&
                            "Media" != table.Name &&
                            "MsiPatchMetadata" != table.Name &&
                            "MsiPatchSequence" != table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(WixErrors.UnexpectedTableInPatch(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;
                    case OutputType.Product:
                        if ("ModuleAdminExecuteSequence" == table.Name ||
                            "ModuleAdminUISequence" == table.Name ||
                            "ModuleAdvtExecuteSequence" == table.Name ||
                            "ModuleAdvtUISequence" == table.Name ||
                            "ModuleComponents" == table.Name ||
                            "ModuleConfiguration" == table.Name ||
                            "ModuleDependency" == table.Name ||
                            "ModuleExclusion" == table.Name ||
                            "ModuleIgnoreTable" == table.Name ||
                            "ModuleInstallExecuteSequence" == table.Name ||
                            "ModuleInstallUISequence" == table.Name ||
                            "ModuleSignature" == table.Name ||
                            "ModuleSubstitution" == table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(WixWarnings.UnexpectedTableInProduct(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;
                }
            }
        }
#endif

#if MOVE_TO_BACKEND
        /// <summary>
        /// Performs various consistency checks on the output.
        /// </summary>
        /// <param name="output">Output containing instance transform definitions.</param>
        private void CheckOutputConsistency(Output output)
        {
            // Get the output's minimum installer version
            int outputInstallerVersion = int.MinValue;
            Table summaryInformationTable = output.Tables["_SummaryInformation"];
            if (null != summaryInformationTable)
            {
                foreach (Row row in summaryInformationTable.Rows)
                {
                    if (14 == (int)row[0])
                    {
                        outputInstallerVersion = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }

            // ensure the Error table exists if output is marked for MSI 1.0 or below (see ICE40)
            if (100 >= outputInstallerVersion && OutputType.Product == output.Type)
            {
                output.EnsureTable(this.tableDefinitions["Error"]);
            }

            // check for the presence of tables/rows/columns that require MSI 1.1 or later
            if (110 > outputInstallerVersion)
            {
                Table isolatedComponentTable = output.Tables["IsolatedComponent"];
                if (null != isolatedComponentTable)
                {
                    foreach (Row row in isolatedComponentTable.Rows)
                    {
                        this.Messaging.Write(WixWarnings.TableIncompatibleWithInstallerVersion(row.SourceLineNumbers, "IsolatedComponent", outputInstallerVersion));
                    }
                }
            }

            // check for the presence of tables/rows/columns that require MSI 4.0 or later
            if (400 > outputInstallerVersion)
            {
                Table shortcutTable = output.Tables["Shortcut"];
                if (null != shortcutTable)
                {
                    foreach (Row row in shortcutTable.Rows)
                    {
                        if (null != row[12] || null != row[13] || null != row[14] || null != row[15])
                        {
                            this.Messaging.Write(WixWarnings.ColumnsIncompatibleWithInstallerVersion(row.SourceLineNumbers, "Shortcut", outputInstallerVersion));
                        }
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Load the standard action symbols.
        /// </summary>
        /// <param name="symbols">Collection of symbols.</param>
        private void LoadStandardActionSymbols(IntermediateSection section, IDictionary<string, Symbol> symbols)
        {
            foreach (var actionRow in WindowsInstallerStandard.StandardActions())
            {
                var symbol = new Symbol(section, actionRow);

                // If the action's symbol has not already been defined (i.e. overriden by the user), add it now.
                if (!symbols.ContainsKey(symbol.Name))
                {
                    symbols.Add(symbol.Name, symbol);
                }
            }
        }

        /// <summary>
        /// Process the complex references.
        /// </summary>
        /// <param name="resolvedSection">Active section to add tuples to.</param>
        /// <param name="sections">Sections that are referenced during the link process.</param>
        /// <param name="referencedComponents">Collection of all components referenced by complex reference.</param>
        /// <param name="componentsToFeatures">Component to feature complex references.</param>
        /// <param name="featuresToFeatures">Feature to feature complex references.</param>
        /// <param name="modulesToFeatures">Module to feature complex references.</param>
        private void ProcessComplexReferences(IntermediateSection resolvedSection, IEnumerable<IntermediateSection> sections, ISet<string> referencedComponents, ConnectToFeatureCollection componentsToFeatures, ConnectToFeatureCollection featuresToFeatures, ConnectToFeatureCollection modulesToFeatures)
        {
            var componentsToModules = new Hashtable();

            foreach (var section in sections)
            {
                // Need ToList since we might want to add tuples while processing.
                foreach (var wixComplexReferenceRow in section.Tuples.OfType<WixComplexReferenceTuple>().ToList())
                {
                    ConnectToFeature connection;
                    switch (wixComplexReferenceRow.ParentType)
                    {
                        case ComplexReferenceParentType.Feature:
                            switch (wixComplexReferenceRow.ChildType)
                            {
                                case ComplexReferenceChildType.Component:
                                    connection = componentsToFeatures[wixComplexReferenceRow.Child];
                                    if (null == connection)
                                    {
                                        componentsToFeatures.Add(new ConnectToFeature(section, wixComplexReferenceRow.Child, wixComplexReferenceRow.Parent, wixComplexReferenceRow.IsPrimary));
                                    }
                                    else if (wixComplexReferenceRow.IsPrimary)
                                    {
                                        if (connection.IsExplicitPrimaryFeature)
                                        {
                                            this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Product"), connection.PrimaryFeature ?? resolvedSection.Id));
                                            continue;
                                        }
                                        else
                                        {
                                            connection.ConnectFeatures.Add(connection.PrimaryFeature); // move the guessed primary feature to the list of connects
                                            connection.PrimaryFeature = wixComplexReferenceRow.Parent; // set the new primary feature
                                            connection.IsExplicitPrimaryFeature = true; // and make sure we remember that we set it so we can fail if we try to set it again
                                        }
                                    }
                                    else
                                    {
                                        connection.ConnectFeatures.Add(wixComplexReferenceRow.Parent);
                                    }

                                    // add a row to the FeatureComponents table
                                    section.AddTuple(new FeatureComponentsTuple
                                    {
                                        FeatureRef = wixComplexReferenceRow.Parent,
                                        ComponentRef = wixComplexReferenceRow.Child,
                                    });

                                    // index the component for finding orphaned records
                                    var symbolName = String.Concat("Component:", wixComplexReferenceRow.Child);
                                    referencedComponents.Add(symbolName);

                                    break;

                                case ComplexReferenceChildType.Feature:
                                    connection = featuresToFeatures[wixComplexReferenceRow.Child];
                                    if (null != connection)
                                    {
                                        this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Product"), (null != connection.PrimaryFeature ? connection.PrimaryFeature : resolvedSection.Id)));
                                        continue;
                                    }

                                    featuresToFeatures.Add(new ConnectToFeature(section, wixComplexReferenceRow.Child, wixComplexReferenceRow.Parent, wixComplexReferenceRow.IsPrimary));
                                    break;

                                case ComplexReferenceChildType.Module:
                                    connection = modulesToFeatures[wixComplexReferenceRow.Child];
                                    if (null == connection)
                                    {
                                        modulesToFeatures.Add(new ConnectToFeature(section, wixComplexReferenceRow.Child, wixComplexReferenceRow.Parent, wixComplexReferenceRow.IsPrimary));
                                    }
                                    else if (wixComplexReferenceRow.IsPrimary)
                                    {
                                        if (connection.IsExplicitPrimaryFeature)
                                        {
                                            this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Product"), (null != connection.PrimaryFeature ? connection.PrimaryFeature : resolvedSection.Id)));
                                            continue;
                                        }
                                        else
                                        {
                                            connection.ConnectFeatures.Add(connection.PrimaryFeature); // move the guessed primary feature to the list of connects
                                            connection.PrimaryFeature = wixComplexReferenceRow.Parent; // set the new primary feature
                                            connection.IsExplicitPrimaryFeature = true; // and make sure we remember that we set it so we can fail if we try to set it again
                                        }
                                    }
                                    else
                                    {
                                        connection.ConnectFeatures.Add(wixComplexReferenceRow.Parent);
                                    }
                                    break;

                                default:
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Unexpected complex reference child type: {0}", Enum.GetName(typeof(ComplexReferenceChildType), wixComplexReferenceRow.ChildType)));
                            }
                            break;

                        case ComplexReferenceParentType.Module:
                            switch (wixComplexReferenceRow.ChildType)
                            {
                                case ComplexReferenceChildType.Component:
                                    if (componentsToModules.ContainsKey(wixComplexReferenceRow.Child))
                                    {
                                        this.Messaging.Write(ErrorMessages.ComponentReferencedTwice(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.Child));
                                        continue;
                                    }
                                    else
                                    {
                                        componentsToModules.Add(wixComplexReferenceRow.Child, wixComplexReferenceRow); // should always be new

                                        // add a row to the ModuleComponents table
                                        section.AddTuple(new ModuleComponentsTuple
                                        {
                                            Component = wixComplexReferenceRow.Child,
                                            ModuleID = wixComplexReferenceRow.Parent,
                                            Language = Convert.ToInt32(wixComplexReferenceRow.ParentLanguage),
                                        });
                                    }

                                    // index the component for finding orphaned records
                                    var componentSymbolName = String.Concat("Component:", wixComplexReferenceRow.Child);
                                    referencedComponents.Add(componentSymbolName);

                                    break;

                                default:
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Unexpected complex reference child type: {0}", Enum.GetName(typeof(ComplexReferenceChildType), wixComplexReferenceRow.ChildType)));
                            }
                            break;

                        case ComplexReferenceParentType.Patch:
                            switch (wixComplexReferenceRow.ChildType)
                            {
                                case ComplexReferenceChildType.PatchFamily:
                                case ComplexReferenceChildType.PatchFamilyGroup:
                                    break;

                                default:
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Unexpected complex reference child type: {0}", Enum.GetName(typeof(ComplexReferenceChildType), wixComplexReferenceRow.ChildType)));
                            }
                            break;

                        case ComplexReferenceParentType.Product:
                            switch (wixComplexReferenceRow.ChildType)
                            {
                                case ComplexReferenceChildType.Feature:
                                    connection = featuresToFeatures[wixComplexReferenceRow.Child];
                                    if (null != connection)
                                    {
                                        this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Product"), (null != connection.PrimaryFeature ? connection.PrimaryFeature : resolvedSection.Id)));
                                        continue;
                                    }

                                    featuresToFeatures.Add(new ConnectToFeature(section, wixComplexReferenceRow.Child, null, wixComplexReferenceRow.IsPrimary));
                                    break;

                                default:
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Unexpected complex reference child type: {0}", Enum.GetName(typeof(ComplexReferenceChildType), wixComplexReferenceRow.ChildType)));
                            }
                            break;

                        default:
                            // Note: Groups have been processed before getting here so they are not handled by any case above.
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Unexpected complex reference child type: {0}", Enum.GetName(typeof(ComplexReferenceParentType), wixComplexReferenceRow.ParentType)));
                    }
                }
            }
        }

        /// <summary>
        /// Flattens all complex references in all sections in the collection.
        /// </summary>
        /// <param name="sections">Sections that are referenced during the link process.</param>
        private void FlattenSectionsComplexReferences(IEnumerable<IntermediateSection> sections)
        {
            var parentGroups = new Dictionary<string, List<WixComplexReferenceTuple>>();
            var parentGroupsSections = new Dictionary<string, IntermediateSection>();
            var parentGroupsNeedingProcessing = new Dictionary<string, IntermediateSection>();

            // DisplaySectionComplexReferences("--- section's complex references before flattening ---", sections);

            // Step 1:  Gather all of the complex references that are going to participate
            // in the flatting process. This means complex references that have "grouping
            //  parents" of Features, Modules, and, of course, Groups. These references
            // that participate in a "grouping parent" will be removed from their section
            // now and after processing added back in Step 3 below.
            foreach (var section in sections)
            {
                // Count down because we'll sometimes remove items from the list.
                for (var i = section.Tuples.Count - 1; i >= 0; --i)
                {
                    // Only process the "grouping parents" such as FeatureGroup, ComponentGroup, Feature,
                    // and Module. Non-grouping complex references are simple and
                    // resolved during normal complex reference resolutions.
                    if (section.Tuples[i] is WixComplexReferenceTuple wixComplexReferenceRow &&
                        (ComplexReferenceParentType.FeatureGroup == wixComplexReferenceRow.ParentType ||
                         ComplexReferenceParentType.ComponentGroup == wixComplexReferenceRow.ParentType ||
                         ComplexReferenceParentType.Feature == wixComplexReferenceRow.ParentType ||
                         ComplexReferenceParentType.Module == wixComplexReferenceRow.ParentType ||
                         ComplexReferenceParentType.PatchFamilyGroup == wixComplexReferenceRow.ParentType ||
                         ComplexReferenceParentType.Product == wixComplexReferenceRow.ParentType))
                    {
                        var parentTypeAndId = this.CombineTypeAndId(wixComplexReferenceRow.ParentType, wixComplexReferenceRow.Parent);

                        // Group all complex references with a common parent
                        // together so we can find them quickly while processing in
                        // Step 2.
                        if (!parentGroups.TryGetValue(parentTypeAndId, out var childrenComplexRefs))
                        {
                            childrenComplexRefs = new List<WixComplexReferenceTuple>();
                            parentGroups.Add(parentTypeAndId, childrenComplexRefs);
                        }

                        childrenComplexRefs.Add(wixComplexReferenceRow);
                        section.Tuples.RemoveAt(i);

                        // Remember the mapping from set of complex references with a common
                        // parent to their section. We'll need this to add them back to the
                        // correct section in Step 3.
                        if (!parentGroupsSections.TryGetValue(parentTypeAndId, out var parentSection))
                        {
                            parentGroupsSections.Add(parentTypeAndId, section);
                        }

                        // If the child of the complex reference is another group, then in Step 2
                        // we're going to have to process this complex reference again to copy
                        // the child group's references into the parent group.
                        if ((ComplexReferenceChildType.ComponentGroup == wixComplexReferenceRow.ChildType) ||
                            (ComplexReferenceChildType.FeatureGroup == wixComplexReferenceRow.ChildType) ||
                            (ComplexReferenceChildType.PatchFamilyGroup == wixComplexReferenceRow.ChildType))
                        {
                            if (!parentGroupsNeedingProcessing.ContainsKey(parentTypeAndId))
                            {
                                parentGroupsNeedingProcessing.Add(parentTypeAndId, section);
                            }
                        }
                    }
                }
            }

            Debug.Assert(parentGroups.Count == parentGroupsSections.Count);
            Debug.Assert(parentGroupsNeedingProcessing.Count <= parentGroups.Count);

            // DisplaySectionComplexReferences("\r\n\r\n--- section's complex references middle of flattening ---", sections);

            // Step 2:  Loop through the parent groups that have nested groups removing
            // them from the hash table as they are processed. At the end of this the
            // complex references should all be flattened.
            var keys = parentGroupsNeedingProcessing.Keys.ToList();

            foreach (var key in keys)
            {
                if (parentGroupsNeedingProcessing.ContainsKey(key))
                {
                    var loopDetector = new Stack<string>();
                    this.FlattenGroup(key, loopDetector, parentGroups, parentGroupsNeedingProcessing);
                }
                else
                {
                    // the group must have allready been procesed and removed from the hash table
                }
            }
            Debug.Assert(0 == parentGroupsNeedingProcessing.Count);

            // Step 3:  Finally, ensure that all of the groups that were removed
            // in Step 1 and flattened in Step 2 are added to their appropriate
            // section. This is where we will toss out the final no-longer-needed
            // groups.
            foreach (var parentGroup in parentGroups.Keys)
            {
                var section = parentGroupsSections[parentGroup];

                foreach (var wixComplexReferenceRow in parentGroups[parentGroup])
                {
                    if ((ComplexReferenceParentType.FeatureGroup != wixComplexReferenceRow.ParentType) &&
                        (ComplexReferenceParentType.ComponentGroup != wixComplexReferenceRow.ParentType) &&
                        (ComplexReferenceParentType.PatchFamilyGroup != wixComplexReferenceRow.ParentType))
                    {
                        section.AddTuple(wixComplexReferenceRow);
                    }
                }
            }

            // DisplaySectionComplexReferences("\r\n\r\n--- section's complex references after flattening ---", sections);
        }

        private string CombineTypeAndId(ComplexReferenceParentType type, string id)
        {
            return String.Concat(type.ToString(), ":", id);
        }

        private string CombineTypeAndId(ComplexReferenceChildType type, string id)
        {
            return String.Concat(type.ToString(), ":", id);
        }

        /// <summary>
        /// Recursively processes the group.
        /// </summary>
        /// <param name="parentTypeAndId">String combination type and id of group to process next.</param>
        /// <param name="loopDetector">Stack of groups processed thus far. Used to detect loops.</param>
        /// <param name="parentGroups">Hash table of complex references grouped by parent id.</param>
        /// <param name="parentGroupsNeedingProcessing">Hash table of parent groups that still have nested groups that need to be flattened.</param>
        private void FlattenGroup(string parentTypeAndId, Stack<string> loopDetector, Dictionary<string, List<WixComplexReferenceTuple>> parentGroups, Dictionary<string, IntermediateSection> parentGroupsNeedingProcessing)
        {
            Debug.Assert(parentGroupsNeedingProcessing.ContainsKey(parentTypeAndId));
            loopDetector.Push(parentTypeAndId); // push this complex reference parent identfier into the stack for loop verifying

            var allNewChildComplexReferences = new List<WixComplexReferenceTuple>();

            var referencesToParent = parentGroups[parentTypeAndId];
            foreach (var wixComplexReferenceRow in referencesToParent)
            {
                Debug.Assert(ComplexReferenceParentType.ComponentGroup == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.FeatureGroup == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.Feature == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.Module == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.Product == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.PatchFamilyGroup == wixComplexReferenceRow.ParentType || ComplexReferenceParentType.Patch == wixComplexReferenceRow.ParentType);
                Debug.Assert(parentTypeAndId == this.CombineTypeAndId(wixComplexReferenceRow.ParentType, wixComplexReferenceRow.Parent));

                // We are only interested processing when the child is a group.
                if ((ComplexReferenceChildType.ComponentGroup == wixComplexReferenceRow.ChildType) ||
                    (ComplexReferenceChildType.FeatureGroup == wixComplexReferenceRow.ChildType) ||
                    (ComplexReferenceChildType.PatchFamilyGroup == wixComplexReferenceRow.ChildType))
                {
                    var childTypeAndId = this.CombineTypeAndId(wixComplexReferenceRow.ChildType, wixComplexReferenceRow.Child);
                    if (loopDetector.Contains(childTypeAndId))
                    {
                        // Create a comma delimited list of the references that participate in the
                        // loop for the error message. Start at the bottom of the stack and work the
                        // way up to present the loop as a directed graph.
                        var loop = String.Join(" -> ", loopDetector);

                        this.Messaging.Write(ErrorMessages.ReferenceLoopDetected(wixComplexReferenceRow?.SourceLineNumbers, loop));

                        // Cleanup the parentGroupsNeedingProcessing and the loopDetector just like the
                        // exit of this method does at the end because we are exiting early.
                        loopDetector.Pop();
                        parentGroupsNeedingProcessing.Remove(parentTypeAndId);

                        return; // bail
                    }

                    // Check to see if the child group still needs to be processed. If so,
                    // go do that so that we'll get all of that children's (and children's
                    // children) complex references correctly merged into our parent group.
                    if (parentGroupsNeedingProcessing.ContainsKey(childTypeAndId))
                    {
                        this.FlattenGroup(childTypeAndId, loopDetector, parentGroups, parentGroupsNeedingProcessing);
                    }

                    // If the child is a parent to anything (i.e. the parent has grandchildren)
                    // clone each of the children's complex references, repoint them to the parent
                    // complex reference (because we're moving references up the tree), and finally
                    // add the cloned child's complex reference to the list of complex references
                    // that we'll eventually add to the parent group.
                    if (parentGroups.TryGetValue(childTypeAndId, out var referencesToChild))
                    {
                        foreach (var crefChild in referencesToChild)
                        {
                            // Only merge up the non-group items since groups are purged
                            // after this part of the processing anyway (cloning them would
                            // be a complete waste of time).
                            if ((ComplexReferenceChildType.FeatureGroup != crefChild.ChildType) ||
                                (ComplexReferenceChildType.ComponentGroup != crefChild.ChildType) ||
                                (ComplexReferenceChildType.PatchFamilyGroup != crefChild.ChildType))
                            {
                                var crefChildClone = crefChild.Clone();
                                Debug.Assert(crefChildClone.Parent == wixComplexReferenceRow.Child);

                                crefChildClone.Reparent(wixComplexReferenceRow);
                                allNewChildComplexReferences.Add(crefChildClone);
                            }
                        }
                    }
                }
            }

            // Add the children group's complex references to the parent
            // group. Clean out any left over groups and quietly remove any
            // duplicate complex references that occurred during the merge.
            referencesToParent.AddRange(allNewChildComplexReferences);
            referencesToParent.Sort(ComplexReferenceComparision);
            for (var i = referencesToParent.Count - 1; i >= 0; --i)
            {
                var wixComplexReferenceRow = referencesToParent[i];

                if ((ComplexReferenceChildType.FeatureGroup == wixComplexReferenceRow.ChildType) ||
                    (ComplexReferenceChildType.ComponentGroup == wixComplexReferenceRow.ChildType) ||
                    (ComplexReferenceChildType.PatchFamilyGroup == wixComplexReferenceRow.ChildType))
                {
                    referencesToParent.RemoveAt(i);
                }
                else if (i > 0)
                {
                    // Since the list is already sorted, we can find duplicates by simply
                    // looking at the next sibling in the list and tossing out one if they
                    // match.
                    var crefCompare = referencesToParent[i - 1];
                    if (0 == wixComplexReferenceRow.CompareToWithoutConsideringPrimary(crefCompare))
                    {
                        referencesToParent.RemoveAt(i);
                    }
                }
            }

            int ComplexReferenceComparision(WixComplexReferenceTuple x, WixComplexReferenceTuple y)
            {
                var comparison = x.ChildType - y.ChildType;
                if (0 == comparison)
                {
                    comparison = String.Compare(x.Child, y.Child, StringComparison.Ordinal);
                    if (0 == comparison)
                    {
                        comparison = x.ParentType - y.ParentType;
                        if (0 == comparison)
                        {
                            comparison = String.Compare(x.ParentLanguage ?? String.Empty, y.ParentLanguage ?? String.Empty, StringComparison.Ordinal);
                            if (0 == comparison)
                            {
                                comparison = String.Compare(x.Parent, y.Parent, StringComparison.Ordinal);
                            }
                        }
                    }
                }

                return comparison;
            }

            loopDetector.Pop(); // pop this complex reference off the stack since we're done verify the loop here
            parentGroupsNeedingProcessing.Remove(parentTypeAndId); // remove the newly processed complex reference
        }

        /*
                /// <summary>
                /// Debugging method for displaying the section complex references.
                /// </summary>
                /// <param name="header">The header.</param>
                /// <param name="sections">The sections to display.</param>
                private void DisplaySectionComplexReferences(string header, SectionCollection sections)
                {
                    Console.WriteLine(header);
                    foreach (Section section in sections)
                    {
                        Table wixComplexReferenceTable = section.Tables["WixComplexReference"];

                        foreach (WixComplexReferenceRow cref in wixComplexReferenceTable.Rows)
                        {
                            Console.WriteLine("Section: {0} Parent: {1} Type: {2} Child: {3} Primary: {4}", section.Id, cref.ParentId, cref.ParentType, cref.ChildId, cref.IsPrimary);
                        }
                    }
                }
        */

        /// <summary>
        /// Flattens the tables used in a Bundle.
        /// </summary>
        /// <param name="output">Output containing the tables to process.</param>
        private void FlattenBundleTables(IntermediateSection entrySection)
        {
            if (SectionType.Bundle != entrySection.Type)
            {
                return;
            }

            // We need to flatten the nested PayloadGroups and PackageGroups under
            // UX, Chain, and any Containers. When we're done, the WixGroups table
            // will hold Payloads under UX, ChainPackages (references?) under Chain,
            // and ChainPackages/Payloads under the attached and any detatched
            // Containers.
            var groups = new WixGroupingOrdering(entrySection, this.Messaging);

            // Create UX payloads and Package payloads
            groups.UseTypes(new[] { ComplexReferenceParentType.Container, ComplexReferenceParentType.Layout, ComplexReferenceParentType.PackageGroup, ComplexReferenceParentType.PayloadGroup, ComplexReferenceParentType.Package }, new[] { ComplexReferenceChildType.PackageGroup, ComplexReferenceChildType.Package, ComplexReferenceChildType.PayloadGroup, ComplexReferenceChildType.Payload });
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Package, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Container, false);
            groups.FlattenAndRewriteGroups(ComplexReferenceParentType.Layout, false);

            // Create Chain packages...
            groups.UseTypes(new[] { ComplexReferenceParentType.PackageGroup }, new[] { ComplexReferenceChildType.Package, ComplexReferenceChildType.PackageGroup });
            groups.FlattenAndRewriteRows(ComplexReferenceChildType.PackageGroup, "WixChain", false);

            groups.RemoveUsedGroupRows();
        }

        /// <summary>
        /// Resolves the features connected to other features in the active output.
        /// </summary>
        /// <param name="featuresToFeatures">Feature to feature complex references.</param>
        /// <param name="allSymbols">All symbols loaded from the sections.</param>
        private void ResolveFeatureToFeatureConnects(ConnectToFeatureCollection featuresToFeatures, IDictionary<string, Symbol> allSymbols)
        {
            foreach (ConnectToFeature connection in featuresToFeatures)
            {
                var wixSimpleReferenceRow = new WixSimpleReferenceTuple
                {
                    Table = "Feature",
                    PrimaryKeys = connection.ChildId
                };

                if (allSymbols.TryGetValue(wixSimpleReferenceRow.SymbolicName, out var symbol))
                {
                    var featureTuple = (FeatureTuple)symbol.Row;
                    featureTuple.ParentFeatureRef = connection.PrimaryFeature;
                }
            }
        }

#if DEAD_CODE
        /// <summary>
        /// Copies a table's rows to an output table.
        /// </summary>
        /// <param name="table">Source table to copy rows from.</param>
        /// <param name="outputTable">Destination table in output to copy rows into.</param>
        /// <param name="sectionId">Id of the section that the table lives in.</param>
        private void CopyTableRowsToOutputTable(Table table, Table outputTable, string sectionId)
        {
            int[] localizedColumns = new int[table.Definition.Columns.Count];
            int localizedColumnCount = 0;

            // if there are localization strings, figure out which columns can be localized in this table
            if (null != this.Localizer)
            {
                for (int i = 0; i < table.Definition.Columns.Count; i++)
                {
                    if (table.Definition.Columns[i].IsLocalizable)
                    {
                        localizedColumns[localizedColumnCount++] = i;
                    }
                }
            }

            // process each row in the table doing the string resource substitutions
            // then add the row to the output
            foreach (Row row in table.Rows)
            {
                for (int j = 0; j < localizedColumnCount; j++)
                {
                    Field field = row.Fields[localizedColumns[j]];

                    if (null != field.Data)
                    {
                        field.Data = this.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, (string)field.Data, true);
                    }
                }

                row.SectionId = (this.sectionIdOnRows ? sectionId : null);
                outputTable.Rows.Add(row);
            }
        }
#endif


        /// <summary>
        /// Resolve features for columns that have null guid placeholders.
        /// </summary>
        /// <param name="tuple">Tuple to resolve.</param>
        /// <param name="connectionColumn">Number of the column containing the connection identifier.</param>
        /// <param name="featureColumn">Number of the column containing the feature.</param>
        /// <param name="connectToFeatures">Connect to feature complex references.</param>
        /// <param name="multipleFeatureComponents">Hashtable of known components under multiple features.</param>
        private void ResolveFeatures(IntermediateTuple tuple, int connectionColumn, int featureColumn, ConnectToFeatureCollection connectToFeatures, Hashtable multipleFeatureComponents)
        {
            var connectionId = connectionColumn < 0 ? tuple.Id.Id : tuple.AsString(connectionColumn);
            var featureId = tuple.AsString(featureColumn);

            if (EmptyGuid == featureId)
            {
                var connection = connectToFeatures[connectionId];

                if (null == connection)
                {
                    // display an error for the component or merge module as appropriate
                    if (null != multipleFeatureComponents)
                    {
                        this.Messaging.Write(ErrorMessages.ComponentExpectedFeature(tuple.SourceLineNumbers, connectionId, tuple.Definition.Name, tuple.Id.Id));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.MergeModuleExpectedFeature(tuple.SourceLineNumbers, connectionId));
                    }
                }
                else
                {
                    // check for unique, implicit, primary feature parents with multiple possible parent features
                    if (this.ShowPedanticMessages &&
                        !connection.IsExplicitPrimaryFeature &&
                        0 < connection.ConnectFeatures.Count)
                    {
                        // display a warning for the component or merge module as approrpriate
                        if (null != multipleFeatureComponents)
                        {
                            if (!multipleFeatureComponents.Contains(connectionId))
                            {
                                this.Messaging.Write(WarningMessages.ImplicitComponentPrimaryFeature(connectionId));

                                // remember this component so only one warning is generated for it
                                multipleFeatureComponents[connectionId] = null;
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ImplicitMergeModulePrimaryFeature(connectionId));
                        }
                    }

                    // set the feature
                    tuple.Set(featureColumn, connection.PrimaryFeature);
                }
            }
        }
    }
}
