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

    /// <summary>
    /// Core librarian tool.
    /// </summary>
    public sealed class Librarian
    {
        private ILibraryContext Context { get; set; }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <param name="sections">The sections to combine into a library.</param>
        /// <returns>Returns the new library.</returns>
        public Intermediate Combine(ILibraryContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));

            if (String.IsNullOrEmpty(this.Context.LibraryId))
            {
                this.Context.LibraryId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            }

            foreach (var extension in this.Context.Extensions)
            {
                extension.PreCombine(this.Context);
            }

            var sections = this.Context.Intermediates.SelectMany(i => i.Sections).ToList();

            var fileResolver = new FileResolver(this.Context.BindPaths, this.Context.Extensions);

            var embedFilePaths = ResolveFilePathsToEmbed(sections, fileResolver);

            var localizationsByCulture = CollateLocalizations(this.Context.Localizations);

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
            FindEntrySectionAndLoadSymbolsCommand find = new FindEntrySectionAndLoadSymbolsCommand(library.Sections);
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

            return (Messaging.Instance.EncounteredError ? null : library);
        }

        private static Dictionary<string, Localization> CollateLocalizations(IEnumerable<Localization> localizations)
        {
            var localizationsByCulture = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);

            foreach (var localization in localizations)
            {
                if (localizationsByCulture.TryGetValue(localization.Culture, out var existingCulture))
                {
                    existingCulture.Merge(localization);
                }
                else
                {
                    localizationsByCulture.Add(localization.Culture, localization);
                }
            }

            return localizationsByCulture;
        }

        private List<string> ResolveFilePathsToEmbed(IEnumerable<IntermediateSection> sections, FileResolver fileResolver)
        {
            var embedFilePaths = new List<string>();

            // Resolve paths to files that are to be embedded in the library.
            if (this.Context.BindFiles)
            {
                foreach (var tuple in sections.SelectMany(s => s.Tuples))
                {
                    foreach (var field in tuple.Fields.Where(f => f.Type == IntermediateFieldType.Path))
                    {
                        var pathField = field.AsPath();

                        if (pathField != null)
                        {
                            var resolvedPath = this.Context.WixVariableResolver.ResolveVariables(tuple.SourceLineNumbers, pathField.Path, false);

                            var file = fileResolver.Resolve(tuple.SourceLineNumbers, tuple.Definition.Name, resolvedPath);

                            if (!String.IsNullOrEmpty(file))
                            {
                                // File was successfully resolved so track the embedded index as the embedded file index.
                                field.Set(new IntermediateFieldPathValue { EmbeddedFileIndex = embedFilePaths.Count });

                                embedFilePaths.Add(file);
                            }
                            else
                            {
                                this.Context.Messaging.OnMessage(WixDataErrors.FileNotFound(tuple.SourceLineNumbers, pathField.Path, tuple.Definition.Name));
                            }
                        }
                    }
                }
            }

            return embedFilePaths;
        }
    }
}
