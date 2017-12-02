// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class BuildCommand : ICommandLineCommand
    {
        public BuildCommand(IServiceProvider serviceProvider, IExtensionManager extensions, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, IEnumerable<string> libraryFiles, string outputPath, OutputType outputType, string cabCachePath, IEnumerable<string> cultures, bool bindFiles, IEnumerable<BindPath> bindPaths, string intermediateFolder, string contentsFile, string outputsFile, string builtOutputsFile, string wixProjectFile)
        {
            this.ServiceProvider = serviceProvider;
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

        public IServiceProvider ServiceProvider { get; }

        public IExtensionManager ExtensionManager { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

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

            if (!intermediates.Any())
            {
                return 1;
            }

            if (this.OutputType == OutputType.Library)
            {
                var library = this.LibraryPhase(intermediates);

                library?.Save(this.OutputPath);
            }
            else
            {
                var output = this.LinkPhase(intermediates);

                if (!Messaging.Instance.EncounteredError)
                {
                    this.BindPhase(output);
                }
            }

            return Messaging.Instance.LastErrorNumber;
        }

        private IEnumerable<Intermediate> CompilePhase()
        {
            var intermediates = new List<Intermediate>();

            foreach (var sourceFile in this.SourceFiles)
            {
                var preprocessContext = this.ServiceProvider.GetService<IPreprocessContext>();
                preprocessContext.Messaging = Messaging.Instance;
                preprocessContext.Extensions = this.ExtensionManager.Create<IPreprocessorExtension>();
                preprocessContext.Platform = Platform.X86; // TODO: set this correctly
                preprocessContext.IncludeSearchPaths = this.IncludeSearchPaths?.ToList() ?? new List<string>();
                preprocessContext.SourceFile = sourceFile.SourcePath;
                preprocessContext.Variables = new Dictionary<string, string>(this.PreprocessorVariables);

                var preprocessor = new Preprocessor();
                var document = preprocessor.Process(preprocessContext);

                var compileContext = this.ServiceProvider.GetService<ICompileContext>();
                compileContext.Messaging = Messaging.Instance;
                compileContext.CompilationId = Guid.NewGuid().ToString("N");
                compileContext.Extensions = this.ExtensionManager.Create<ICompilerExtension>();
                compileContext.OutputPath = sourceFile.OutputPath;
                compileContext.Platform = Platform.X86; // TODO: set this correctly
                compileContext.Source = document;

                var compiler = new Compiler();
                var intermediate = compiler.Compile(compileContext);

                intermediates.Add(intermediate);
            }

            return intermediates;
        }

        private Intermediate LibraryPhase(IEnumerable<Intermediate> intermediates)
        {
            var localizations = this.LoadLocalizationFiles().ToList();

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
            context.LibraryId = Guid.NewGuid().ToString("N");
            context.Intermediates = intermediates;
            context.WixVariableResolver = resolver;

            var librarian = new Librarian();
            return librarian.Combine(context);
        }

        private Intermediate LinkPhase(IEnumerable<Intermediate> intermediates)
        {
            var creator = this.ServiceProvider.GetService<ITupleDefinitionCreator>();

            var libraries = this.LoadLibraries(creator);

            var context = this.ServiceProvider.GetService<ILinkContext>();
            context.Messaging = Messaging.Instance;
            context.Extensions = this.ExtensionManager.Create<ILinkerExtension>();
            context.Intermediates = intermediates.Union(libraries).ToList();
            context.ExpectedOutputType = this.OutputType;

            var linker = new Linker();
            var output = linker.Link(context);
            return output;
        }

        private void BindPhase(Intermediate output)
        {
            var localizations = this.LoadLocalizationFiles().ToList();

            var localizer = new Localizer(localizations);

            var resolver = CreateWixResolverWithVariables(localizer, output);

            var intermediateFolder = this.IntermediateFolder;
            if (String.IsNullOrEmpty(intermediateFolder))
            {
                intermediateFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }

            var context = this.ServiceProvider.GetService<IBindContext>();
            context.Messaging = Messaging.Instance;
            context.ExtensionManager = this.ExtensionManager;
            context.BindPaths = this.BindPaths ?? Array.Empty<BindPath>();
            //context.CabbingThreadCount = this.CabbingThreadCount;
            context.CabCachePath = this.CabCachePath;
            context.Codepage = localizer.Codepage;
            //context.DefaultCompressionLevel = this.DefaultCompressionLevel;
            //context.Ices = this.Ices;
            context.IntermediateFolder = intermediateFolder;
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

            var binder = new Binder();
            binder.Bind(context);
        }

        private IEnumerable<Intermediate> LoadLibraries(ITupleDefinitionCreator creator)
        {
            var libraries = new List<Intermediate>();

            if (this.LibraryFiles != null)
            {
                foreach (var libraryFile in this.LibraryFiles)
                {
                    try
                    {
                        var library = Intermediate.Load(libraryFile, creator);

                        libraries.Add(library);
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

            return libraries;
        }

        private IEnumerable<Localization> LoadLocalizationFiles()
        {
            foreach (var loc in this.LocFiles)
            {
                var localization = Localizer.ParseLocalizationFile(loc);

                yield return localization;
            }
        }

        private static WixVariableResolver CreateWixResolverWithVariables(Localizer localizer, Intermediate output)
        {
            var resolver = new WixVariableResolver(localizer);

            // Gather all the wix variables.
            var wixVariables = output?.Sections.SelectMany(s => s.Tuples).OfType<WixVariableTuple>();
            if (wixVariables != null)
            {
                foreach (var wixVariableRow in wixVariables)
                {
                    resolver.AddVariable(wixVariableRow);
                }
            }

            return resolver;
        }
    }
}
