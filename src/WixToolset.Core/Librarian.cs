// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Link;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Core librarian tool.
    /// </summary>
    internal class Librarian : ILibrarian
    {
        internal Librarian(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <returns>Returns the new library.</returns>
        public Intermediate Combine(ILibraryContext context)
        {
            if (String.IsNullOrEmpty(context.LibraryId))
            {
                context.LibraryId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            }

            foreach (var extension in context.Extensions)
            {
                extension.PreCombine(context);
            }

            Intermediate library = null;
            try
            {
                var sections = context.Intermediates.SelectMany(i => i.Sections).ToList();

                var collate = new CollateLocalizationsCommand(this.Messaging, context.Localizations);
                var localizationsByCulture = collate.Execute();

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                this.ResolveFilePathsToEmbed(context, sections);

                foreach (var section in sections)
                {
                    section.LibraryId = context.LibraryId;
                }

                library = new Intermediate(context.LibraryId, sections, localizationsByCulture);

                this.Validate(library);
            }
            finally
            {
                foreach (var extension in context.Extensions)
                {
                    extension.PostCombine(library);
                }
            }

            return this.Messaging.EncounteredError ? null : library;
        }

        private void ResolveFilePathsToEmbed(ILibraryContext context, IEnumerable<IntermediateSection> sections)
        {
            // Resolve paths to files that are to be embedded in the library.
            if (context.BindFiles)
            {
                var variableResolver = this.ServiceProvider.GetService<IVariableResolver>();

                var fileResolver = new FileResolver(context.BindPaths, context.Extensions);

                foreach (var tuple in sections.SelectMany(s => s.Tuples))
                {
                    foreach (var field in tuple.Fields.Where(f => f?.Type == IntermediateFieldType.Path))
                    {
                        var pathField = field.AsPath();

                        if (pathField != null && !String.IsNullOrEmpty(pathField.Path))
                        {
                            var resolution = variableResolver.ResolveVariables(tuple.SourceLineNumbers, pathField.Path);

                            var file = fileResolver.Resolve(tuple.SourceLineNumbers, tuple.Definition, resolution.Value);

                            if (!String.IsNullOrEmpty(file))
                            {
                                // File was successfully resolved so track the embedded index as the embedded file index.
                                field.Set(new IntermediateFieldPathValue { Embed = true, Path = file });
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.FileNotFound(tuple.SourceLineNumbers, pathField.Path, tuple.Definition.Name));
                            }
                        }
                    }
                }
            }
        }

        private void Validate(Intermediate library)
        {
            var find = new FindEntrySectionAndLoadSymbolsCommand(this.Messaging, library.Sections, OutputType.Library);
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
        }
    }
}
