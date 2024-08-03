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
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Linker core of the WiX toolset.
    /// </summary>
    internal class Linker : ILinker
    {
        private static readonly string EmptyGuid = Guid.Empty.ToString("B");

        /// <summary>
        /// Creates a linker.
        /// </summary>
        internal Linker(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

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

            if (this.Context.SymbolDefinitionCreator == null)
            {
                this.Context.SymbolDefinitionCreator = this.ServiceProvider.GetService<ISymbolDefinitionCreator>();
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
                    var library = data.GetLibrary(this.Context.SymbolDefinitionCreator);

                    if (library != null)
                    {
                        sections.AddRange(library.Sections);

                        if (library.Localizations?.Count > 0)
                        {
                            // Include localizations from the extension data and be sure to note that the localization came from
                            // an extension. It is important to remember which localization came from an extension when filtering
                            // localizations during the resolve process later.
                            localizations.AddRange(library.Localizations.Select(l => l.UpdateLocation(LocalizationLocation.Extension)));
                        }
                    }
                }

                // Load the standard wixlib.
                if (!this.Context.SkipStdWixlib)
                {
                    var stdlib = WixStandardLibrary.Build(this.Context.Platform);

                    sections.AddRange(stdlib.Sections);

                    if (stdlib.Localizations?.Count > 0)
                    {
                        localizations.AddRange(stdlib.Localizations);
                    }
                }

                var multipleFeatureComponents = new Hashtable();

                var wixVariables = new Dictionary<string, WixVariableSymbol>();

                // First find the entry section and while processing all sections load all the symbols from all of the sections.
                var find = new FindEntrySectionAndLoadSymbolsCommand(this.Messaging, sections, this.Context.ExpectedOutputType);
                find.Execute();

                // Must have found the entry section by now.
                if (null == find.EntrySection)
                {
                    if (this.Context.ExpectedOutputType == OutputType.IntermediatePostLink || this.Context.ExpectedOutputType == OutputType.Unknown)
                    {
                        throw new WixException(ErrorMessages.MissingEntrySection());
                    }
                    else
                    {
                        throw new WixException(ErrorMessages.MissingEntrySection(this.Context.ExpectedOutputType.ToString()));
                    }
                }

                // Add default symbols that need a bit more intelligence than just being
                // included in the standard library.
                {
                    var command = new AddDefaultSymbolsCommand(find, sections);
                    command.Execute();
                }

                // If there are no authored features, create a default feature and assign
                // the components to it.
                {
                    var command = new AssignDefaultFeatureCommand(find, sections);
                    command.Execute();
                }

                // Resolve the symbol references to find the set of sections we care about for linking.
                // Of course, we start with the entry section (that's how it got its name after all).
                var resolve = new ResolveReferencesCommand(this.Messaging, find.EntrySection, find.SymbolsByName);
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

                // If there are authored features, error for any referenced components that aren't assigned to a feature.
                foreach (var component in sections.SelectMany(s => s.Symbols.Where(y => y.Definition.Type == SymbolDefinitionType.Component)))
                {
                    if (!referencedComponents.Contains(component.Id.Id))
                    {
                        this.Messaging.Write(ErrorMessages.OrphanedComponent(component.SourceLineNumbers, component.Id.Id));
                    }
                }

                // Process conflicts that may be overridden virtual symbols (that's okay) or end up as primary key collisions (those need to be reported as errors).
                ISet<IntermediateSymbol> overriddenSymbols;
                {
                    var reportDupes = new ProcessConflictingSymbolsCommand(this.Messaging, find.PossibleConflicts, find.OverrideSymbols, resolve.ResolvedSections);
                    reportDupes.Execute();

                    overriddenSymbols = reportDupes.OverriddenSymbols;
                }

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // resolve the feature to feature connects
                this.ResolveFeatureToFeatureConnects(featuresToFeatures, find.SymbolsByName);

                // Create a new section to hold the linked content. Start with the entry section's
                // metadata.
                var resolvedSection = new IntermediateSection(find.EntrySection.Id, find.EntrySection.Type);
                var identicalDirectoryIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var section in sections)
                {
                    foreach (var symbol in section.Symbols)
                    {
                        // If this symbol is an identical directory, ensure we only visit
                        // one (and skip the other identicals with the same id).
                        if (find.IdenticalDirectorySymbols.Contains(symbol))
                        {
                            if (!identicalDirectoryIds.Add(symbol.Id.Id))
                            {
                                continue;
                            }
                        }
                        else if (overriddenSymbols.Contains(symbol))
                        {
                            // Skip the symbols that were overridden.
                            continue;
                        }

                        var copySymbol = true; // by default, copy symbols.

                        // handle special tables
                        switch (symbol.Definition.Type)
                        {
                            case SymbolDefinitionType.Class:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)ClassSymbolFields.ComponentRef, (int)ClassSymbolFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.Extension:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)ExtensionSymbolFields.ComponentRef, (int)ExtensionSymbolFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.Assembly:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)AssemblySymbolFields.ComponentRef, (int)AssemblySymbolFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.PublishComponent:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)PublishComponentSymbolFields.ComponentRef, (int)PublishComponentSymbolFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.Shortcut:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)ShortcutSymbolFields.ComponentRef, (int)ShortcutSymbolFields.Target, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.TypeLib:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, (int)TypeLibSymbolFields.ComponentRef, (int)TypeLibSymbolFields.FeatureRef, componentsToFeatures, multipleFeatureComponents);
                                }
                                break;

                            case SymbolDefinitionType.WixMerge:
                                if (SectionType.Package == resolvedSection.Type)
                                {
                                    this.ResolveFeatures(symbol, -1, (int)WixMergeSymbolFields.FeatureRef, modulesToFeatures, null);
                                }
                                break;

                            case SymbolDefinitionType.WixSimpleReference:
                            case SymbolDefinitionType.WixComplexReference:
                                copySymbol = false;
                                break;

                            case SymbolDefinitionType.WixVariable:
                                this.AddWixVariable(wixVariables, (WixVariableSymbol)symbol);
                                copySymbol = false; // Do not copy the symbol, it will be added later after all overriding has been handled.
                                break;
                        }

                        if (copySymbol)
                        {
                            resolvedSection.AddSymbol(symbol);
                        }
                    }
                }

                // Copy the module to feature connections into the output.
                foreach (ConnectToFeature connectToFeature in modulesToFeatures)
                {
                    foreach (var feature in connectToFeature.ConnectFeatures)
                    {
                        resolvedSection.AddSymbol(new WixFeatureModulesSymbol
                        {
                            FeatureRef = feature,
                            WixMergeRef = connectToFeature.ChildId
                        });
                    }
                }

                // Copy the wix variable rows to the output now that all overriding has been accounted for.
                foreach (var symbol in wixVariables.Values)
                {
                    resolvedSection.AddSymbol(symbol);
                }

                // Bundles have groups of data that must be flattened in a way different from other types.
                if (resolvedSection.Type == SectionType.Bundle)
                {
                    var command = new FlattenAndProcessBundleTablesCommand(resolvedSection, this.Messaging);
                    command.Execute();
                }

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                var collate = new CollateLocalizationsCommand(this.Messaging, localizations);
                var localizationsByCulture = collate.Execute();

                intermediate = new Intermediate(resolvedSection.Id, Data.IntermediateLevels.Linked, new[] { resolvedSection }, localizationsByCulture);
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

        /// <summary>
        /// Check for colliding values and collect the wix variable rows.
        /// </summary>
        /// <param name="wixVariables">Collection of WixVariableSymbols by id.</param>
        /// <param name="symbol">WixVariableSymbol to add, if not overridden.</param>
        private void AddWixVariable(Dictionary<string, WixVariableSymbol> wixVariables, WixVariableSymbol symbol)
        {
            var id = symbol.Id.Id;

            if (wixVariables.TryGetValue(id, out var collidingSymbol))
            {
                if (collidingSymbol.Overridable && !symbol.Overridable)
                {
                    wixVariables[id] = symbol;
                }
                else if (!symbol.Overridable || (collidingSymbol.Overridable && symbol.Overridable))
                {
                    this.Messaging.Write(ErrorMessages.BindVariableCollision(symbol.SourceLineNumbers, id));
                }
            }
            else
            {
                wixVariables.Add(id, symbol);
            }
        }

        /// <summary>
        /// Process the complex references.
        /// </summary>
        /// <param name="resolvedSection">Active section to add symbols to.</param>
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
                // Need ToList since we might want to add symbols while processing.
                var wixComplexReferences = section.Symbols.OfType<WixComplexReferenceSymbol>().ToList();
                foreach (var wixComplexReferenceRow in wixComplexReferences)
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
                                            this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Package"), connection.PrimaryFeature ?? resolvedSection.Id));
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
                                    section.AddSymbol(new FeatureComponentsSymbol
                                    {
                                        FeatureRef = wixComplexReferenceRow.Parent,
                                        ComponentRef = wixComplexReferenceRow.Child,
                                    });

                                    // index the component for finding orphaned records
                                    referencedComponents.Add(wixComplexReferenceRow.Child);

                                    break;

                                case ComplexReferenceChildType.Feature:
                                    connection = featuresToFeatures[wixComplexReferenceRow.Child];
                                    if (null != connection)
                                    {
                                        this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Package"), connection.PrimaryFeature ?? resolvedSection.Id));
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
                                            this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Package"), connection.PrimaryFeature ?? resolvedSection.Id));
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
                                        section.AddSymbol(new ModuleComponentsSymbol
                                        {
                                            Component = wixComplexReferenceRow.Child,
                                            ModuleID = wixComplexReferenceRow.Parent,
                                            Language = Convert.ToInt32(wixComplexReferenceRow.ParentLanguage),
                                        });
                                    }

                                    // index the component for finding orphaned records
                                    referencedComponents.Add(wixComplexReferenceRow.Child);

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
                                        this.Messaging.Write(ErrorMessages.MultiplePrimaryReferences(wixComplexReferenceRow.SourceLineNumbers, wixComplexReferenceRow.ChildType.ToString(), wixComplexReferenceRow.Child, wixComplexReferenceRow.ParentType.ToString(), wixComplexReferenceRow.Parent, (null != connection.PrimaryFeature ? "Feature" : "Package"), connection.PrimaryFeature ?? resolvedSection.Id));
                                        continue;
                                    }

                                    featuresToFeatures.Add(new ConnectToFeature(section, wixComplexReferenceRow.Child, null, wixComplexReferenceRow.IsPrimary));
                                    break;

                                case ComplexReferenceChildType.Component:
                                case ComplexReferenceChildType.ComponentGroup:
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
            var parentGroups = new Dictionary<string, List<WixComplexReferenceSymbol>>();
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
                var removeSymbols = new List<IntermediateSymbol>();

                foreach (var symbol in section.Symbols)
                {
                    // Only process the "grouping parents" such as FeatureGroup, ComponentGroup, Feature,
                    // and Module. Non-grouping complex references are simple and
                    // resolved during normal complex reference resolutions.
                    if (symbol is WixComplexReferenceSymbol wixComplexReferenceRow &&
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
                            childrenComplexRefs = new List<WixComplexReferenceSymbol>();
                            parentGroups.Add(parentTypeAndId, childrenComplexRefs);
                        }

                        childrenComplexRefs.Add(wixComplexReferenceRow);
                        removeSymbols.Add(wixComplexReferenceRow);

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

                foreach (var removeSymbol in removeSymbols)
                {
                    section.RemoveSymbol(removeSymbol);
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
                        section.AddSymbol(wixComplexReferenceRow);
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
        private void FlattenGroup(string parentTypeAndId, Stack<string> loopDetector, Dictionary<string, List<WixComplexReferenceSymbol>> parentGroups, Dictionary<string, IntermediateSection> parentGroupsNeedingProcessing)
        {
            Debug.Assert(parentGroupsNeedingProcessing.ContainsKey(parentTypeAndId));
            loopDetector.Push(parentTypeAndId); // push this complex reference parent identfier into the stack for loop verifying

            var allNewChildComplexReferences = new List<WixComplexReferenceSymbol>();

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
                        // Create an arrow-delimited list of the references that participate in the
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

            int ComplexReferenceComparision(WixComplexReferenceSymbol x, WixComplexReferenceSymbol y)
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
        /// Resolves the features connected to other features in the active output.
        /// </summary>
        /// <param name="featuresToFeatures">Feature to feature complex references.</param>
        /// <param name="allSymbols">All symbols loaded from the sections.</param>
        private void ResolveFeatureToFeatureConnects(ConnectToFeatureCollection featuresToFeatures, IDictionary<string, SymbolWithSection> allSymbols)
        {
            foreach (ConnectToFeature connection in featuresToFeatures)
            {
                var wixSimpleReferenceRow = new WixSimpleReferenceSymbol
                {
                    Table = "Feature",
                    PrimaryKeys = connection.ChildId
                };

                if (allSymbols.TryGetValue(wixSimpleReferenceRow.SymbolicName, out var symbol))
                {
                    var featureSymbol = (FeatureSymbol)symbol.Symbol;
                    featureSymbol.ParentFeatureRef = connection.PrimaryFeature;
                }
            }
        }

        /// <summary>
        /// Resolve features for columns that have null guid placeholders.
        /// </summary>
        /// <param name="symbol">Symbol to resolve.</param>
        /// <param name="connectionColumn">Number of the column containing the connection identifier.</param>
        /// <param name="featureColumn">Number of the column containing the feature.</param>
        /// <param name="connectToFeatures">Connect to feature complex references.</param>
        /// <param name="multipleFeatureComponents">Hashtable of known components under multiple features.</param>
        private void ResolveFeatures(IntermediateSymbol symbol, int connectionColumn, int featureColumn, ConnectToFeatureCollection connectToFeatures, Hashtable multipleFeatureComponents)
        {
            var connectionId = connectionColumn < 0 ? symbol.Id.Id : symbol.AsString(connectionColumn);
            var featureId = symbol.AsString(featureColumn);

            if (EmptyGuid == featureId)
            {
                var connection = connectToFeatures[connectionId];

                if (null == connection)
                {
                    // display an error for the component or merge module as appropriate
                    if (null != multipleFeatureComponents)
                    {
                        this.Messaging.Write(ErrorMessages.ComponentExpectedFeature(symbol.SourceLineNumbers, connectionId, symbol.Definition.Name, symbol.Id.Id));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.MergeModuleExpectedFeature(symbol.SourceLineNumbers, connectionId));
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
                    symbol.Set(featureColumn, connection.PrimaryFeature);
                }
            }
        }
    }
}
