// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Link;

    /// <summary>
    /// Core librarian tool.
    /// </summary>
    public sealed class Librarian
    {
        public Librarian(LibraryContext context)
        {
            this.Context = context;
        }

        private LibraryContext Context { get; }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <param name="sections">The sections to combine into a library.</param>
        /// <returns>Returns the new library.</returns>
        public Library Combine()
        {
            foreach (var extension in this.Context.Extensions)
            {
                extension.PreCombine(this.Context);
            }

            var fileResolver = new FileResolver(this.Context.BindPaths, this.Context.Extensions);

            var localizationsByCulture = CollateLocalizations(this.Context.Localizations);

            var embedFilePaths = ResolveFilePathsToEmbed(this.Context.Sections, fileResolver);

            var library = new Library(this.Context.Sections, localizationsByCulture, embedFilePaths);

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
        private Library Validate(Library library)
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

        private List<string> ResolveFilePathsToEmbed(IEnumerable<Section> sections, FileResolver fileResolver)
        {
            var embedFilePaths = new List<string>();

            // Resolve paths to files that are to be embedded in the library.
            if (this.Context.BindFiles)
            {
                foreach (Table table in sections.SelectMany(s => s.Tables))
                {
                    foreach (Row row in table.Rows)
                    {
                        foreach (ObjectField objectField in row.Fields.OfType<ObjectField>())
                        {
                            if (null != objectField.Data)
                            {
                                string resolvedPath = this.Context.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, (string)objectField.Data, false);

                                string file = fileResolver.Resolve(row.SourceLineNumbers, table.Name, resolvedPath);

                                if (!String.IsNullOrEmpty(file))
                                {
                                    // File was successfully resolved so track the embedded index as the embedded file index.
                                    objectField.EmbeddedFileIndex = embedFilePaths.Count;
                                    embedFilePaths.Add(file);
                                }
                                else
                                {
                                    Messaging.Instance.OnMessage(WixDataErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.Data, table.Name));
                                }
                            }
                            else // clear out embedded file id in case there was one there before.
                            {
                                objectField.EmbeddedFileIndex = null;
                            }
                        }
                    }
                }
            }

            return embedFilePaths;
        }
    }
}
