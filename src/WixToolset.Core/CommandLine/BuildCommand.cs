// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    internal class BuildCommand : ICommandLineCommand
    {
        public BuildCommand(ExtensionManager extensions, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, IEnumerable<string> libraryFiles, string outputPath, OutputType outputType, string cabCachePath, IEnumerable<string> cultures, bool bindFiles, IEnumerable<BindPath> bindPaths, string intermediateFolder, string contentsFile, string outputsFile, string builtOutputsFile, string wixProjectFile)
        {
            this.ExtensionManager = extensions;
            this.LocFiles = locFiles;
            this.LibraryFiles = libraryFiles;
            this.PreprocessorVariables = preprocessorVariables;
            this.SourceFiles = sources;
            this.OutputPath = outputPath;
            this.OutputType = outputType;

            this.CabCachePath = cabCachePath;
            this.Cultures = cultures;
            this.BindFiles = bindFiles;
            this.BindPaths = bindPaths;

            this.IntermediateFolder = intermediateFolder ?? Path.GetTempPath();
            this.ContentsFile = contentsFile;
            this.OutputsFile = outputsFile;
            this.BuiltOutputsFile = builtOutputsFile;
            this.WixProjectFile = wixProjectFile;
        }

        public ExtensionManager ExtensionManager { get; }

        public IEnumerable<string> LocFiles { get; }

        public IEnumerable<string> LibraryFiles { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private string OutputPath { get; }

        private OutputType OutputType { get; }

        public string CabCachePath { get; }

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
                var library = this.LibraryPhase(intermediates, tableDefinitions);

                library?.Save(this.OutputPath);
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

        private Library LibraryPhase(IEnumerable<Intermediate> intermediates, TableDefinitionCollection tableDefinitions)
        {
            var localizations = this.LoadLocalizationFiles(tableDefinitions).ToList();

            // If there was an error adding localization files, then bail.
            if (Messaging.Instance.EncounteredError)
            {
                return null;
            }

            var resolver = CreateWixResolverWithVariables(null, null);

            var context = new LibraryContext();
            context.BindFiles = this.BindFiles;
            context.BindPaths = this.BindPaths;
            context.Extensions = this.ExtensionManager.Create<ILibrarianExtension>();
            context.Localizations = localizations;
            context.Sections = intermediates.SelectMany(i => i.Sections).ToList();
            context.WixVariableResolver = resolver;

            var librarian = new Librarian(context);

            return librarian.Combine();
        }

        private Output LinkPhase(IEnumerable<Intermediate> intermediates, TableDefinitionCollection tableDefinitions)
        {
            var sections = intermediates.SelectMany(i => i.Sections).ToList();

            sections.AddRange(this.SectionsFromLibraries(tableDefinitions));

            var linker = new Linker();

            foreach (var data in this.ExtensionManager.Create<IExtensionData>())
            {
                linker.AddExtensionData(data);
            }

            var output = linker.Link(sections, this.OutputType);

            return output;
        }

        private void BindPhase(Output output, TableDefinitionCollection tableDefinitions)
        {
            var localizations = this.LoadLocalizationFiles(tableDefinitions).ToList();

            var localizer = new Localizer(localizations);

            var resolver = CreateWixResolverWithVariables(localizer, output);

            var context = new BindContext();
            context.Messaging = Messaging.Instance;
            context.ExtensionManager = this.ExtensionManager;
            context.BindPaths = this.BindPaths ?? Array.Empty<BindPath>();
            //context.CabbingThreadCount = this.CabbingThreadCount;
            context.CabCachePath = this.CabCachePath;
            context.Codepage = localizer.Codepage;
            //context.DefaultCompressionLevel = this.DefaultCompressionLevel;
            //context.Ices = this.Ices;
            context.IntermediateFolder = this.IntermediateFolder;
            context.IntermediateRepresentation = output;
            context.OutputPath = this.OutputPath;
            context.OutputPdbPath = Path.ChangeExtension(this.OutputPath, ".wixpdb");
            //context.SuppressIces = this.SuppressIces;
            context.SuppressValidation = true;
            //context.SuppressValidation = this.SuppressValidation;
            context.WixVariableResolver = resolver;
            context.ContentsFile = this.ContentsFile;
            context.OutputsFile = this.OutputsFile;
            context.BuiltOutputsFile = this.BuiltOutputsFile;
            context.WixprojectFile = this.WixProjectFile;

            var binder = new Binder(context);
            binder.Bind();
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

        private IEnumerable<Localization> LoadLocalizationFiles(TableDefinitionCollection tableDefinitions)
        {
            foreach (var loc in this.LocFiles)
            {
                var localization = Localizer.ParseLocalizationFile(loc, tableDefinitions);

                yield return localization;
            }
        }

        private static WixVariableResolver CreateWixResolverWithVariables(Localizer localizer, Output output)
        {
            var resolver = new WixVariableResolver(localizer);

            // Gather all the wix variables.
            Table wixVariableTable = output?.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    resolver.AddVariable(wixVariableRow);
                }
            }

            return resolver;
        }
    }
}
