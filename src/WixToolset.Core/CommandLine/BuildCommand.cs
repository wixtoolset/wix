// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BuildCommand : ICommandLineCommand
    {
        public BuildCommand(IServiceProvider serviceProvider, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, IEnumerable<string> libraryFiles, IEnumerable<string> filterCultures, string outputPath, OutputType outputType, string cabCachePath, bool bindFiles, IEnumerable<BindPath> bindPaths, IEnumerable<string> includeSearchPaths, string intermediateFolder, string contentsFile, string outputsFile, string builtOutputsFile)
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
                var preprocessor = new Preprocessor(this.ServiceProvider);
                preprocessor.IncludeSearchPaths = this.IncludeSearchPaths;
                preprocessor.Platform = Platform.X86; // TODO: set this correctly
                preprocessor.SourcePath = sourceFile.SourcePath;
                preprocessor.Variables = this.PreprocessorVariables;

                XDocument document = null;
                try
                {
                    document = preprocessor.Execute();
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var compiler = new Compiler(this.ServiceProvider);
                compiler.OutputPath = sourceFile.OutputPath;
                compiler.Platform = Platform.X86; // TODO: set this correctly
                compiler.SourceDocument = document;
                var intermediate = compiler.Execute();

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
            var librarian = new Librarian(this.ServiceProvider);
            librarian.BindFiles = this.BindFiles;
            librarian.BindPaths = this.BindPaths;
            librarian.Intermediates = intermediates;
            librarian.Localizations = localizations;
            return librarian.Execute();
        }

        private Intermediate LinkPhase(IEnumerable<Intermediate> intermediates, ITupleDefinitionCreator creator)
        {
            var libraries = this.LoadLibraries(creator);

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            var linker = new Linker(this.ServiceProvider);
            linker.OutputType = this.OutputType;
            linker.Intermediates = intermediates;
            linker.Libraries = libraries;
            linker.TupleDefinitionCreator = creator;
            return linker.Execute();
        }

        private void BindPhase(Intermediate output, IEnumerable<Localization> localizations)
        {
            ResolveResult resolveResult;
            {
                var resolver = new Resolver(this.ServiceProvider);
                resolver.BindPaths = this.BindPaths;
                resolver.FilterCultures = this.FilterCultures;
                resolver.IntermediateFolder = this.IntermediateFolder;
                resolver.IntermediateRepresentation = output;
                resolver.Localizations = localizations;

                resolveResult = resolver.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            BindResult bindResult;
            {
                var intermediateFolder = this.IntermediateFolder;
                if (String.IsNullOrEmpty(intermediateFolder))
                {
                    intermediateFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                }

                var binder = new Binder(this.ServiceProvider);
                //binder.CabbingThreadCount = this.CabbingThreadCount;
                binder.CabCachePath = this.CabCachePath;
                binder.Codepage = resolveResult.Codepage;
                //binder.DefaultCompressionLevel = this.DefaultCompressionLevel;
                binder.DelayedFields = resolveResult.DelayedFields;
                binder.ExpectedEmbeddedFiles = resolveResult.ExpectedEmbeddedFiles;
                binder.Ices = Array.Empty<string>(); // TODO: set this correctly
                binder.IntermediateFolder = intermediateFolder;
                binder.IntermediateRepresentation = resolveResult.IntermediateRepresentation;
                binder.OutputPath = this.OutputPath;
                binder.OutputPdbPath = Path.ChangeExtension(this.OutputPath, ".wixpdb");
                binder.SuppressIces = Array.Empty<string>(); // TODO: set this correctly
                binder.SuppressValidation = true; // TODO: set this correctly

                bindResult = binder.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            {
                var layout = new Layout(this.ServiceProvider);
                layout.TrackedFiles = bindResult.TrackedFiles;
                layout.FileTransfers = bindResult.FileTransfers;
                layout.IntermediateFolder = this.IntermediateFolder;
                layout.ContentsFile = this.ContentsFile;
                layout.OutputsFile = this.OutputsFile;
                layout.BuiltOutputsFile = this.BuiltOutputsFile;
                layout.SuppressAclReset = false; // TODO: correctly set SuppressAclReset

                layout.Execute();
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
                var preprocessor = new Preprocessor(this.ServiceProvider);
                preprocessor.IncludeSearchPaths = this.IncludeSearchPaths;
                preprocessor.Platform = Platform.X86; // TODO: set this correctly
                preprocessor.SourcePath = loc;
                preprocessor.Variables = this.PreprocessorVariables;
                var document = preprocessor.Execute();

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var localization = localizer.ParseLocalizationFile(document);
                yield return localization;
            }
        }
    }
}
