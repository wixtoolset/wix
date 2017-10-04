// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    internal class BuildCommand : ICommand
    {
        public BuildCommand(ExtensionManager extensions, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, IEnumerable<string> libraryFiles, string outputPath, OutputType outputType, IEnumerable<string> cultures, bool bindFiles, IEnumerable<BindPath> bindPaths, string intermediateFolder, string contentsFile, string outputsFile, string builtOutputsFile, string wixProjectFile)
        {
            this.Extensions = extensions;
            this.LocFiles = locFiles;
            this.LibraryFiles = libraryFiles;
            this.PreprocessorVariables = preprocessorVariables;
            this.SourceFiles = sources;
            this.OutputPath = outputPath;
            this.OutputType = outputType;

            this.Cultures = cultures;
            this.BindFiles = bindFiles;
            this.BindPaths = bindPaths;

            this.IntermediateFolder = intermediateFolder ?? Path.GetTempPath();
            this.ContentsFile = contentsFile;
            this.OutputsFile = outputsFile;
            this.BuiltOutputsFile = builtOutputsFile;
            this.WixProjectFile = wixProjectFile;
        }

        public ExtensionManager Extensions { get; }

        public IEnumerable<string> LocFiles { get; }

        public IEnumerable<string> LibraryFiles { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private string OutputPath { get; }

        private OutputType OutputType { get; }

        public IEnumerable<string> Cultures { get; }

        public bool BindFiles { get; }

        public IEnumerable<BindPath> BindPaths { get; }

        public string IntermediateFolder { get; }

        public string ContentsFile { get; }

        public string OutputsFile { get; }

        public string BuiltOutputsFile { get; }

        public string WixProjectFile { get; }

        public int Execute()
        {
            var intermediates = this.CompilePhase();

            var tableDefinitions = new TableDefinitionCollection(WindowsInstallerStandard.GetTableDefinitions());

            if (this.OutputType == OutputType.Library)
            {
                this.LibraryPhase(intermediates, tableDefinitions);
            }
            else
            {
                var output = this.LinkPhase(intermediates, tableDefinitions);

                if (!Messaging.Instance.EncounteredError)
                {
                    this.BindPhase(output, tableDefinitions);
                }
            }

            return Messaging.Instance.LastErrorNumber;
        }

        private IEnumerable<Intermediate> CompilePhase()
        {
            var intermediates = new List<Intermediate>();

            var preprocessor = new Preprocessor();

            var compiler = new Compiler();

            foreach (var sourceFile in this.SourceFiles)
            {
                var document = preprocessor.Process(sourceFile.SourcePath, this.PreprocessorVariables);

                var intermediate = compiler.Compile(document);

                intermediates.Add(intermediate);
            }

            return intermediates;
        }

        private void LibraryPhase(IEnumerable<Intermediate> intermediates, TableDefinitionCollection tableDefinitions)
        {
            var localizations = this.LoadLocalizationFiles(tableDefinitions).ToList();

            // If there was an error adding localization files, then bail.
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            var sections = intermediates.SelectMany(i => i.Sections).ToList();

            LibraryBinaryFileResolver resolver = null;

            if (this.BindFiles)
            {
                resolver = new LibraryBinaryFileResolver();
                resolver.FileManagers = new List<IBinderFileManager> { new BinderFileManager() }; ;
                resolver.VariableResolver = new WixVariableResolver();

                BinderFileManagerCore core = new BinderFileManagerCore();
                core.AddBindPaths(this.BindPaths, BindStage.Normal);

                foreach (var fileManager in resolver.FileManagers)
                {
                    fileManager.Core = core;
                }
            }

            var librarian = new Librarian();

            var library = librarian.Combine(sections, localizations, resolver);

            library?.Save(this.OutputPath);
        }

        private Output LinkPhase(IEnumerable<Intermediate> intermediates, TableDefinitionCollection tableDefinitions)
        {
            var sections = intermediates.SelectMany(i => i.Sections).ToList();

            sections.AddRange(SectionsFromLibraries(tableDefinitions));

            var linker = new Linker();

            foreach (var data in this.Extensions.Create<IExtensionData>())
            {
                linker.AddExtensionData(data);
            }

            var output = linker.Link(sections, this.OutputType);

            return output;
        }

        private IEnumerable<Section> SectionsFromLibraries(TableDefinitionCollection tableDefinitions)
        {
            var sections = new List<Section>();

            if (this.LibraryFiles != null)
            {
                foreach (var libraryFile in this.LibraryFiles)
                {
                    try
                    {
                        var library = Library.Load(libraryFile, tableDefinitions, false);

                        sections.AddRange(library.Sections);
                    }
                    catch (WixCorruptFileException e)
                    {
                        Messaging.Instance.OnMessage(e.Error);
                    }
                    catch (WixUnexpectedFileFormatException e)
                    {
                        Messaging.Instance.OnMessage(e.Error);
                    }
                }
            }

            return sections;
        }

        private void BindPhase(Output output, TableDefinitionCollection tableDefinitions)
        {
            var localizations = this.LoadLocalizationFiles(tableDefinitions).ToList();

            var localizer = new Localizer(localizations);

            var resolver = new WixVariableResolver(localizer);

            var binder = new Binder();
            binder.TempFilesLocation = this.IntermediateFolder;
            binder.WixVariableResolver = resolver;
            binder.SuppressValidation = true;

            binder.ContentsFile = this.ContentsFile;
            binder.OutputsFile = this.OutputsFile;
            binder.BuiltOutputsFile = this.BuiltOutputsFile;
            binder.WixprojectFile = this.WixProjectFile;

            if (this.BindPaths != null)
            {
                binder.BindPaths.AddRange(this.BindPaths);
            }

            binder.AddExtension(new BinderFileManager());

            binder.Bind(output, this.OutputPath);
        }

        private IEnumerable<Localization> LoadLocalizationFiles(TableDefinitionCollection tableDefinitions)
        {
            foreach (var loc in this.LocFiles)
            {
                var localization = Localizer.ParseLocalizationFile(loc, tableDefinitions);

                yield return localization;
            }
        }

        /// <summary>
        /// File resolution mechanism to create binary library.
        /// </summary>
        private class LibraryBinaryFileResolver : ILibraryBinaryFileResolver
        {
            public IEnumerable<IBinderFileManager> FileManagers { get; set; }

            public WixVariableResolver VariableResolver { get; set; }

            public string Resolve(SourceLineNumber sourceLineNumber, string table, string path)
            {
                string resolvedPath = this.VariableResolver.ResolveVariables(sourceLineNumber, path, false);

                foreach (IBinderFileManager fileManager in this.FileManagers)
                {
                    string finalPath = fileManager.ResolveFile(resolvedPath, table, sourceLineNumber, BindStage.Normal);
                    if (!String.IsNullOrEmpty(finalPath))
                    {
                        return finalPath;
                    }
                }

                return null;
            }
        }
    }
}
