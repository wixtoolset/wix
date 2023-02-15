// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            this.FileResolver = this.ServiceProvider.GetService<IFileResolver>();
            this.LayoutServices = this.ServiceProvider.GetService<ILayoutServices>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IFileResolver FileResolver { get; }

        private ILayoutServices LayoutServices { get; }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <returns>Returns tracked input files and the new library.</returns>
        public ILibraryResult Combine(ILibraryContext context)
        {
            if (String.IsNullOrEmpty(context.LibraryId))
            {
                context.LibraryId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            }

            foreach (var extension in context.Extensions)
            {
                extension.PreCombine(context);
            }

            ILibraryResult result = this.ServiceProvider.GetService<ILibraryResult>();
            Intermediate library = null;
            IReadOnlyCollection<ITrackedFile> trackedFiles = null;
            try
            {
                var sections = context.Intermediates.SelectMany(i => i.Sections).ToList();

                var collate = new CollateLocalizationsCommand(this.Messaging, context.Localizations);
                var localizationsByCulture = collate.Execute();

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                trackedFiles = this.ResolveFilePathsToEmbed(context, sections);

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                foreach (var section in sections)
                {
                    section.AssignToLibrary(context.LibraryId);
                }

                library = new Intermediate(context.LibraryId, IntermediateLevels.Compiled, sections, localizationsByCulture);

                library.UpdateLevel(IntermediateLevels.Combined);

                this.Validate(library);
            }
            finally
            {
                result.Library = library;
                result.TrackedFiles = trackedFiles;

                foreach (var extension in context.Extensions)
                {
                    extension.PostCombine(result);
                }
            }

            return result;
        }

        private IReadOnlyCollection<ITrackedFile> ResolveFilePathsToEmbed(ILibraryContext context, IEnumerable<IntermediateSection> sections)
        {
            var trackedFiles = new List<ITrackedFile>();

            // Resolve paths to files that are to be embedded in the library.
            if (context.BindFiles)
            {
                var variableResolver = this.ServiceProvider.GetService<IVariableResolver>();

                foreach (var bindVariable in context.BindVariables)
                {
                    variableResolver.AddVariable(null, bindVariable.Key, bindVariable.Value, false);
                }

                var bindPaths = context.BindPaths.Where(b => b.Stage == BindStage.Normal).ToList();

                foreach (var symbol in sections.SelectMany(s => s.Symbols))
                {
                    foreach (var field in symbol.Fields.Where(f => f?.Type == IntermediateFieldType.Path))
                    {
                        var pathField = field.AsPath();

                        if (pathField != null && !String.IsNullOrEmpty(pathField.Path))
                        {
                            var resolution = variableResolver.ResolveVariables(symbol.SourceLineNumbers, pathField.Path);

                            try
                            {
                                var file = this.FileResolver.ResolveFile(resolution.Value, context.Extensions, bindPaths, symbol.SourceLineNumbers, symbol.Definition);

                                // File was successfully resolved so track the embedded index as the embedded file index.
                                field.Set(new IntermediateFieldPathValue { Embed = true, Path = file });

                                trackedFiles.Add(this.LayoutServices.TrackFile(file, TrackedFileType.Input, symbol.SourceLineNumbers));
                            }
                            catch (WixException e)
                            {
                                this.Messaging.Write(e.Error);
                            }
                        }
                    }
                }
            }

            return trackedFiles;
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
