// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Link;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Core librarian tool.
    /// </summary>
    public sealed class Librarian
    {
        public Librarian(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        private ILibraryContext Context { get; set; }

        public bool BindFiles { get; set; }

        public IEnumerable<BindPath> BindPaths { get; set; }

        public IEnumerable<Localization> Localizations { get; set; }

        public IEnumerable<Intermediate> Intermediates { get; set; }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <param name="sections">The sections to combine into a library.</param>
        /// <returns>Returns the new library.</returns>
        public Intermediate Execute()
        {
            this.Context = new LibraryContext(this.ServiceProvider);
            this.Context.Messaging = this.ServiceProvider.GetService<IMessaging>();
            this.Context.BindFiles = this.BindFiles;
            this.Context.BindPaths = this.BindPaths;
            this.Context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().Create<ILibrarianExtension>();
            this.Context.Localizations = this.Localizations;
            this.Context.LibraryId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            this.Context.Intermediates = this.Intermediates;

            foreach (var extension in this.Context.Extensions)
            {
                extension.PreCombine(this.Context);
            }

            var sections = this.Context.Intermediates.SelectMany(i => i.Sections).ToList();

            var embedFilePaths = this.ResolveFilePathsToEmbed(sections);

            var localizationsByCulture = this.CollateLocalizations(this.Context.Localizations);

            if (this.Context.Messaging.EncounteredError)
            {
                return null;
            }

            foreach (var section in sections)
            {
                section.LibraryId = this.Context.LibraryId;
            }

            var library = new Intermediate(this.Context.LibraryId, sections, localizationsByCulture, embedFilePaths);

            this.Validate(library);

            foreach (var extension in this.Context.Extensions)
            {
                extension.PostCombine(library);
            }

            return library;
        }

        /// <summary>
        /// Validate that a library contains one entry section and no duplicate symbols.
        /// </summary>
        /// <param name="library">Library to validate.</param>
        private Intermediate Validate(Intermediate library)
        {
            FindEntrySectionAndLoadSymbolsCommand find = new FindEntrySectionAndLoadSymbolsCommand(this.Context.Messaging, library.Sections);
            find.Execute();

            // TODO: Consider bringing this sort of verification back.
            // foreach (Section section in library.Sections)
            // {
            //     ResolveReferencesCommand resolve = new ResolveReferencesCommand(find.EntrySection, find.Symbols);
            //     resolve.Execute();
            //
            //     ReportDuplicateResolvedSymbolErrorsCommand reportDupes = new ReportDuplicateResolvedSymbolErrorsCommand(find.SymbolsWithDuplicates, resolve.ResolvedSections);
            //     reportDupes.Execute();
            // }

            return (this.Context.Messaging.EncounteredError ? null : library);
        }

        private Dictionary<string, Localization> CollateLocalizations(IEnumerable<Localization> localizations)
        {
            var localizationsByCulture = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);

            foreach (var localization in localizations)
            {
                if (localizationsByCulture.TryGetValue(localization.Culture, out var existingCulture))
                {
                    try
                    {
                        existingCulture.Merge(localization);
                    }
                    catch (WixException e)
                    {
                        this.Context.Messaging.Write(e.Error);
                    }
                }
                else
                {
                    localizationsByCulture.Add(localization.Culture, localization);
                }
            }

            return localizationsByCulture;
        }

        private List<string> ResolveFilePathsToEmbed(IEnumerable<IntermediateSection> sections)
        {
            var embedFilePaths = new List<string>();

            // Resolve paths to files that are to be embedded in the library.
            if (this.Context.BindFiles)
            {
                var variableResolver = new WixVariableResolver(this.Context.Messaging);

                var fileResolver = new FileResolver(this.Context.BindPaths, this.Context.Extensions);

                foreach (var tuple in sections.SelectMany(s => s.Tuples))
                {
                    foreach (var field in tuple.Fields.Where(f => f?.Type == IntermediateFieldType.Path))
                    {
                        var pathField = field.AsPath();

                        if (pathField != null)
                        {
                            var resolution = variableResolver.ResolveVariables(tuple.SourceLineNumbers, pathField.Path, false);

                            var file = fileResolver.Resolve(tuple.SourceLineNumbers, tuple.Definition, resolution.Value);

                            if (!String.IsNullOrEmpty(file))
                            {
                                // File was successfully resolved so track the embedded index as the embedded file index.
                                field.Set(new IntermediateFieldPathValue { EmbeddedFileIndex = embedFilePaths.Count });

                                embedFilePaths.Add(file);
                            }
                            else
                            {
                                this.Context.Messaging.Write(ErrorMessages.FileNotFound(tuple.SourceLineNumbers, pathField.Path, tuple.Definition.Name));
                            }
                        }
                    }
                }
            }

            return embedFilePaths;
        }
    }
}
