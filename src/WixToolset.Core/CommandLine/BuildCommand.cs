// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BuildCommand : ICommandLineCommand
    {
        public BuildCommand(IServiceProvider serviceProvider, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, IEnumerable<string> libraryFiles, IEnumerable<string> filterCultures, string outputPath, OutputType outputType, Platform platform, string cabCachePath, bool bindFiles, IEnumerable<BindPath> bindPaths, IEnumerable<string> includeSearchPaths, string intermediateFolder, string contentsFile, string outputsFile, string builtOutputsFile)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.LocFiles = locFiles;
            this.LibraryFiles = libraryFiles;
            this.FilterCultures = filterCultures;
            this.PreprocessorVariables = preprocessorVariables;
            this.SourceFiles = sources;
            this.OutputPath = outputPath;
            this.OutputType = outputType;
            this.Platform = platform;

            this.CabCachePath = cabCachePath;
            this.BindFiles = bindFiles;
            this.BindPaths = bindPaths;
            this.IncludeSearchPaths = includeSearchPaths;

            this.IntermediateFolder = intermediateFolder ?? Path.GetTempPath();
            this.ContentsFile = contentsFile;
            this.OutputsFile = outputsFile;
            this.BuiltOutputsFile = builtOutputsFile;
        }

        public IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        public IExtensionManager ExtensionManager { get; }

        public IEnumerable<string> FilterCultures { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

        public IEnumerable<string> LocFiles { get; }

        public IEnumerable<string> LibraryFiles { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private string OutputPath { get; }

        private OutputType OutputType { get; }

        private Platform Platform { get; }

        public string CabCachePath { get; }

        public bool BindFiles { get; }

        public IEnumerable<BindPath> BindPaths { get; }

        public string IntermediateFolder { get; }

        public string ContentsFile { get; }

        public string OutputsFile { get; }

        public string BuiltOutputsFile { get; }

        public int Execute()
        {
            var creator = this.ServiceProvider.GetService<ITupleDefinitionCreator>();

            this.EvaluateSourceFiles(creator, out var codeFiles, out var wixipl);

            if (this.Messaging.EncounteredError)
            {
                return this.Messaging.LastErrorNumber;
            }

            var wixobjs = this.CompilePhase(codeFiles);

            var wxls = this.LoadLocalizationFiles().ToList();

            if (this.Messaging.EncounteredError)
            {
                return this.Messaging.LastErrorNumber;
            }

            if (this.OutputType == OutputType.Library)
            {
                var wixlib = this.LibraryPhase(wixobjs, wxls);

                if (!this.Messaging.EncounteredError)
                {
                    wixlib.Save(this.OutputPath);
                }
            }
            else
            {
                if (wixipl == null)
                {
                    wixipl = this.LinkPhase(wixobjs, creator);
                }

                if (!this.Messaging.EncounteredError)
                {
                    if (this.OutputType == OutputType.IntermediatePostLink)
                    {
                        wixipl.Save(this.OutputPath);
                    }
                    else
                    {
                        this.BindPhase(wixipl, wxls);
                    }
                }
            }

            return this.Messaging.LastErrorNumber;
        }

        private void EvaluateSourceFiles(ITupleDefinitionCreator creator, out List<SourceFile> codeFiles, out Intermediate wixipl)
        {
            codeFiles = new List<SourceFile>();

            wixipl = null;

            foreach (var sourceFile in this.SourceFiles)
            {
                var extension = Path.GetExtension(sourceFile.SourcePath);

                if (wixipl != null || ".wxs".Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    codeFiles.Add(sourceFile);
                }
                else
                {
                    try
                    {
                        wixipl = Intermediate.Load(sourceFile.SourcePath, creator);
                    }
                    catch (WixException)
                    {
                        // We'll assume anything that isn't a valid intermediate is source code to compile.
                        codeFiles.Add(sourceFile);
                    }
                }
            }

            if (wixipl == null && codeFiles.Count == 0)
            {
                this.Messaging.Write(ErrorMessages.NoSourceFiles());
            }
            else if (wixipl != null && codeFiles.Count != 0)
            {
                this.Messaging.Write(ErrorMessages.WixiplSourceFileIsExclusive());
            }
        }

        private IEnumerable<Intermediate> CompilePhase(IEnumerable<SourceFile> sourceFiles)
        {
            var intermediates = new List<Intermediate>();

            foreach (var sourceFile in sourceFiles)
            {
                var document = this.Preprocess(sourceFile.SourcePath);

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var context = this.ServiceProvider.GetService<ICompileContext>();
                context.Extensions = this.ExtensionManager.Create<ICompilerExtension>();
                context.OutputPath = sourceFile.OutputPath;
                context.Platform = this.Platform;
                context.Source = document;

                Intermediate intermediate = null;
                try
                {
                    var compiler = this.ServiceProvider.GetService<ICompiler>();
                    intermediate = compiler.Compile(context);
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                intermediates.Add(intermediate);
            }

            return intermediates;
        }

        private Intermediate LibraryPhase(IEnumerable<Intermediate> intermediates, IEnumerable<Localization> localizations)
        {
            var context = this.ServiceProvider.GetService<ILibraryContext>();
            context.BindFiles = this.BindFiles;
            context.BindPaths = this.BindPaths;
            context.Extensions = this.ExtensionManager.Create<ILibrarianExtension>();
            context.Localizations = localizations;
            context.Intermediates = intermediates;

            Intermediate library = null;
            try
            {
                var librarian = this.ServiceProvider.GetService<ILibrarian>();
                library = librarian.Combine(context);
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
            }

            return library;
        }
        
        private Intermediate LinkPhase(IEnumerable<Intermediate> intermediates, ITupleDefinitionCreator creator)
        {
            var libraries = this.LoadLibraries(creator);

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            var context = this.ServiceProvider.GetService<ILinkContext>();
            context.Extensions = this.ExtensionManager.Create<ILinkerExtension>();
            context.ExtensionData = this.ExtensionManager.Create<IExtensionData>();
            context.ExpectedOutputType = this.OutputType;
            context.Intermediates = intermediates.Concat(libraries).ToList();
            context.TupleDefinitionCreator = creator;

            var linker = this.ServiceProvider.GetService<ILinker>();
            return linker.Link(context);
        }

        private void BindPhase(Intermediate output, IEnumerable<Localization> localizations)
        {
            var intermediateFolder = this.IntermediateFolder;
            if (String.IsNullOrEmpty(intermediateFolder))
            {
                intermediateFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }

            ResolveResult resolveResult;
            {
                var context = this.ServiceProvider.GetService<IResolveContext>();
                context.BindPaths = this.BindPaths;
                context.Extensions = this.ExtensionManager.Create<IResolverExtension>();
                context.ExtensionData = this.ExtensionManager.Create<IExtensionData>();
                context.FilterCultures = this.FilterCultures;
                context.IntermediateFolder = intermediateFolder;
                context.IntermediateRepresentation = output;
                context.Localizations = localizations;
                context.VariableResolver = new WixVariableResolver(this.Messaging);

                var resolver = this.ServiceProvider.GetService<IResolver>();
                resolveResult = resolver.Resolve(context);
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            BindResult bindResult;
            {
                var context = this.ServiceProvider.GetService<IBindContext>();
                //context.CabbingThreadCount = this.CabbingThreadCount;
                context.CabCachePath = this.CabCachePath;
                context.Codepage = resolveResult.Codepage;
                //context.DefaultCompressionLevel = this.DefaultCompressionLevel;
                context.DelayedFields = resolveResult.DelayedFields;
                context.ExpectedEmbeddedFiles = resolveResult.ExpectedEmbeddedFiles;
                context.Extensions = this.ExtensionManager.Create<IBinderExtension>();
                context.Ices = Array.Empty<string>(); // TODO: set this correctly
                context.IntermediateFolder = intermediateFolder;
                context.IntermediateRepresentation = resolveResult.IntermediateRepresentation;
                context.OutputPath = this.OutputPath;
                context.OutputPdbPath = Path.ChangeExtension(this.OutputPath, ".wixpdb");
                context.SuppressIces = Array.Empty<string>(); // TODO: set this correctly
                context.SuppressValidation = true; // TODO: set this correctly

                var binder = this.ServiceProvider.GetService<IBinder>();
                bindResult = binder.Bind(context);
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            {
                var context = this.ServiceProvider.GetService<ILayoutContext>();
                context.Extensions = this.ExtensionManager.Create<ILayoutExtension>();
                context.TrackedFiles = bindResult.TrackedFiles;
                context.FileTransfers = bindResult.FileTransfers;
                context.IntermediateFolder = intermediateFolder;
                context.ContentsFile = this.ContentsFile;
                context.OutputsFile = this.OutputsFile;
                context.BuiltOutputsFile = this.BuiltOutputsFile;
                context.SuppressAclReset = false; // TODO: correctly set SuppressAclReset

                var layout = this.ServiceProvider.GetService<ILayoutCreator>();
                layout.Layout(context);
            }
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
                        this.Messaging.Write(e.Error);
                    }
                    catch (WixUnexpectedFileFormatException e)
                    {
                        this.Messaging.Write(e.Error);
                    }
                }
            }

            return libraries;
        }

        private IEnumerable<Localization> LoadLocalizationFiles()
        {
            var localizer = new Localizer(this.ServiceProvider);

            foreach (var loc in this.LocFiles)
            {
                var document = this.Preprocess(loc);

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var localization = localizer.ParseLocalizationFile(document);
                yield return localization;
            }
        }

        private XDocument Preprocess(string sourcePath)
        {
            var context = this.ServiceProvider.GetService<IPreprocessContext>();
            context.Extensions = this.ExtensionManager.Create<IPreprocessorExtension>();
            context.Platform = this.Platform;
            context.IncludeSearchPaths = this.IncludeSearchPaths;
            context.SourcePath = sourcePath;
            context.Variables = this.PreprocessorVariables;

            XDocument document = null;
            try
            {
                var preprocessor = this.ServiceProvider.GetService<IPreprocessor>();
                document = preprocessor.Preprocess(context);
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
            }

            return document;
        }
    }
}
