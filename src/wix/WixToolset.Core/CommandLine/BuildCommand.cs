// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BuildCommand : ICommandLineCommand
    {
        private readonly CommandLine commandLine;

        public BuildCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.commandLine = new CommandLine(this.ServiceProvider, this.Messaging);
        }

        public bool ShowHelp
        {
            get { return this.commandLine.ShowHelp; }
            set { this.commandLine.ShowHelp = value; }
        }

        public bool ShowLogo
        {
            get { return this.commandLine.ShowLogo; }
            set { this.commandLine.ShowLogo = value; }
        }

        // Stop parsing when we've decided to show help.
        public bool StopParsing => this.commandLine.ShowHelp;

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IExtensionManager ExtensionManager { get; }

        private string IntermediateFolder { get; set; }

        private string OutputPath { get; set; }

        private Platform Platform { get; set; }

        private CompressionLevel? DefaultCompressionLevel { get; set; }

        private string TrackingFile { get; set; }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.commandLine.ShowHelp)
            {
                Console.WriteLine("TODO: Show build command help");
                return Task.FromResult(-1);
            }

            this.IntermediateFolder = this.commandLine.CalculateIntermedateFolder();

            this.Platform = this.commandLine.Platform;

            this.TrackingFile = this.commandLine.TrackingFile;

            this.DefaultCompressionLevel = this.commandLine.DefaultCompressionLevel;

            var preprocessorVariables = this.commandLine.CalculatePreprocessorVariables();

            var filterCultures = this.commandLine.CalculateFilterCultures();

            var creator = this.ServiceProvider.GetService<ISymbolDefinitionCreator>();

            var inputsOutputs = this.commandLine.CalculateInputsAndOutputs(creator);

            this.OutputPath = inputsOutputs.OutputPath;

            if (this.Messaging.EncounteredError)
            {
                return Task.FromResult(this.Messaging.LastErrorNumber);
            }

            var wixobjs = this.CompilePhase(preprocessorVariables, inputsOutputs.SourcePaths, this.commandLine.IncludeSearchPaths, cancellationToken);

            var wxls = this.LoadLocalizationFiles(inputsOutputs.LocalizationPaths, preprocessorVariables, this.commandLine.IncludeSearchPaths, cancellationToken);

            if (this.Messaging.EncounteredError)
            {
                return Task.FromResult(this.Messaging.LastErrorNumber);
            }

            if (inputsOutputs.OutputType == OutputType.Library)
            {
                using (new IntermediateFieldContext("wix.lib"))
                {
                    this.LibraryPhase(wixobjs, wxls, inputsOutputs.LibraryPaths, creator, this.commandLine.BindFiles, this.commandLine.BindPaths, inputsOutputs.OutputPath, cancellationToken);
                }
            }
            else
            {
                using (new IntermediateFieldContext("wix.link"))
                {
                    var wixipl = inputsOutputs.Wixipls.SingleOrDefault();

                    if (wixipl == null)
                    {
                        wixipl = this.LinkPhase(wixobjs, inputsOutputs, creator, cancellationToken);
                    }

                    if (!this.Messaging.EncounteredError)
                    {
                        var outputExtension = Path.GetExtension(inputsOutputs.OutputPath);
                        if (String.IsNullOrEmpty(outputExtension) || ".wix" == outputExtension)
                        {
                            var entrySectionType = wixipl.Sections.Single().Type;

                            inputsOutputs.OutputPath = Path.ChangeExtension(inputsOutputs.OutputPath, DefaultExtensionForSectionType(entrySectionType));
                            this.OutputPath = inputsOutputs.OutputPath;
                        }

                        if (inputsOutputs.OutputType == OutputType.IntermediatePostLink)
                        {
                            wixipl.Save(inputsOutputs.OutputPath);
                        }
                        else
                        {
                            using (new IntermediateFieldContext("wix.bind"))
                            {
                                this.BindPhase(wixipl, wxls, filterCultures, this.commandLine.CabCachePath, this.commandLine.BindPaths, inputsOutputs, cancellationToken);
                            }
                        }
                    }
                }
            }

            return Task.FromResult(this.Messaging.LastErrorNumber);
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            return this.commandLine.TryParseArgument(argument, parser);
        }

        private IReadOnlyList<Intermediate> CompilePhase(IDictionary<string, string> preprocessorVariables, IEnumerable<string> sourceFiles, IReadOnlyCollection<string> includeSearchPaths, CancellationToken cancellationToken)
        {
            var intermediates = new List<Intermediate>();

            foreach (var sourceFile in sourceFiles)
            {
                var document = this.Preprocess(preprocessorVariables, sourceFile, includeSearchPaths, cancellationToken);

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var context = this.ServiceProvider.GetService<ICompileContext>();
                context.Extensions = this.ExtensionManager.GetServices<ICompilerExtension>();
                context.IntermediateFolder = this.IntermediateFolder;
                context.OutputPath = this.OutputPath;
                context.Platform = this.Platform;
                context.Source = document;
                context.CancellationToken = cancellationToken;

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

        private void LibraryPhase(IReadOnlyCollection<Intermediate> intermediates, IReadOnlyCollection<Localization> localizations, IEnumerable<string> libraryFiles, ISymbolDefinitionCreator creator, bool bindFiles, IReadOnlyCollection<IBindPath> bindPaths, string outputPath, CancellationToken cancellationToken)
        {
            var libraries = this.LoadLibraries(libraryFiles, creator);

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            var context = this.ServiceProvider.GetService<ILibraryContext>();
            context.BindFiles = bindFiles;
            context.BindPaths = bindPaths;
            context.Extensions = this.ExtensionManager.GetServices<ILibrarianExtension>();
            context.Localizations = localizations;
            context.IntermediateFolder = this.IntermediateFolder;
            context.Intermediates = intermediates.Concat(libraries).ToList();
            context.OutputPath = this.OutputPath;
            context.CancellationToken = cancellationToken;

            try
            {
                var librarian = this.ServiceProvider.GetService<ILibrarian>();
                var result = librarian.Combine(context);

                if (!this.Messaging.EncounteredError)
                {
                    result.Library.Save(outputPath);

                    this.LayoutFiles(result.TrackedFiles, null, cancellationToken);
                }
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
            }
        }

        private Intermediate LinkPhase(IEnumerable<Intermediate> intermediates, InputsAndOutputs inputsOutputs, ISymbolDefinitionCreator creator, CancellationToken cancellationToken)
        {
            var libraries = this.LoadLibraries(inputsOutputs.LibraryPaths, creator);

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            var context = this.ServiceProvider.GetService<ILinkContext>();
            context.Extensions = this.ExtensionManager.GetServices<ILinkerExtension>();
            context.ExtensionData = this.ExtensionManager.GetServices<IExtensionData>();
            context.ExpectedOutputType = inputsOutputs.OutputType;
            context.IntermediateFolder = this.IntermediateFolder;
            context.Intermediates = intermediates.Concat(libraries).ToList();
            context.OutputPath = this.OutputPath;
            context.SymbolDefinitionCreator = creator;
            context.CancellationToken = cancellationToken;

            var linker = this.ServiceProvider.GetService<ILinker>();
            return linker.Link(context);
        }

        private void BindPhase(Intermediate output, IReadOnlyCollection<Localization> localizations, IReadOnlyCollection<string> filterCultures, string cabCachePath, IReadOnlyCollection<IBindPath> bindPaths, InputsAndOutputs inputsOutputs, CancellationToken cancellationToken)
        {
            IResolveResult resolveResult;
            {
                var context = this.ServiceProvider.GetService<IResolveContext>();
                context.BindPaths = bindPaths;
                context.Extensions = this.ExtensionManager.GetServices<IResolverExtension>();
                context.ExtensionData = this.ExtensionManager.GetServices<IExtensionData>();
                context.FilterCultures = filterCultures;
                context.IntermediateFolder = this.IntermediateFolder;
                context.IntermediateRepresentation = output;
                context.Localizations = localizations;
                context.OutputPath = inputsOutputs.OutputPath;
                context.CancellationToken = cancellationToken;

                var resolver = this.ServiceProvider.GetService<IResolver>();
                resolveResult = resolver.Resolve(context);
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            IBindResult bindResult = null;
            try
            {
                {
                    var context = this.ServiceProvider.GetService<IBindContext>();
                    //context.CabbingThreadCount = this.CabbingThreadCount;
                    context.CabCachePath = cabCachePath;
                    context.ResolvedCodepage = resolveResult.Codepage;
                    context.ResolvedSummaryInformationCodepage = resolveResult.SummaryInformationCodepage;
                    context.ResolvedLcid = resolveResult.PackageLcid;
                    context.DefaultCompressionLevel = this.DefaultCompressionLevel;
                    context.DelayedFields = resolveResult.DelayedFields;
                    context.ExpectedEmbeddedFiles = resolveResult.ExpectedEmbeddedFiles;
                    context.Extensions = this.ExtensionManager.GetServices<IBinderExtension>();
                    context.FileSystemExtensions = this.ExtensionManager.GetServices<IFileSystemExtension>();
                    context.IntermediateFolder = this.IntermediateFolder;
                    context.IntermediateRepresentation = resolveResult.IntermediateRepresentation;
                    context.OutputPath = this.OutputPath;
                    context.PdbType = inputsOutputs.PdbType;
                    context.PdbPath = inputsOutputs.PdbPath;
                    context.CancellationToken = cancellationToken;

                    var binder = this.ServiceProvider.GetService<IBinder>();
                    bindResult = binder.Bind(context);
                }

                if (this.Messaging.EncounteredError)
                {
                    return;
                }

                this.LayoutFiles(bindResult.TrackedFiles, bindResult.FileTransfers, cancellationToken);
            }
            finally
            {
                bindResult?.Dispose();
            }
        }

        private void LayoutFiles(IReadOnlyCollection<ITrackedFile> trackedFiles, IReadOnlyCollection<IFileTransfer> fileTransfers, CancellationToken cancellationToken)
        {
            var context = this.ServiceProvider.GetService<ILayoutContext>();
            context.Extensions = this.ExtensionManager.GetServices<ILayoutExtension>();
            context.TrackedFiles = trackedFiles;
            context.FileTransfers = fileTransfers;
            context.IntermediateFolder = this.IntermediateFolder;
            context.OutputPath = this.OutputPath;
            context.TrackingFile = this.TrackingFile;
            context.ResetAcls = this.commandLine.ResetAcls;
            context.CancellationToken = cancellationToken;

            var layout = this.ServiceProvider.GetService<ILayoutCreator>();
            layout.Layout(context);
        }

        private IEnumerable<Intermediate> LoadLibraries(IEnumerable<string> libraryFiles, ISymbolDefinitionCreator creator)
        {
            try
            {
                return Intermediate.Load(libraryFiles, creator);
            }
            catch (WixCorruptFileException e)
            {
                this.Messaging.Write(e.Error);
            }
            catch (WixUnexpectedFileFormatException e)
            {
                this.Messaging.Write(e.Error);
            }

            return Array.Empty<Intermediate>();
        }

        private IReadOnlyList<Localization> LoadLocalizationFiles(IEnumerable<string> locFiles, IDictionary<string, string> preprocessorVariables, IReadOnlyCollection<string> includeSearchPaths, CancellationToken cancellationToken)
        {
            var localizations = new List<Localization>();
            var parser = this.ServiceProvider.GetService<ILocalizationParser>();

            foreach (var loc in locFiles)
            {
                var document = this.Preprocess(preprocessorVariables, loc, includeSearchPaths, cancellationToken);

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var localization = parser.ParseLocalization(document);
                localizations.Add(localization);
            }

            return localizations;
        }

        private XDocument Preprocess(IDictionary<string, string> preprocessorVariables, string sourcePath, IReadOnlyCollection<string> includeSearchPaths, CancellationToken cancellationToken)
        {
            var context = this.ServiceProvider.GetService<IPreprocessContext>();
            context.Extensions = this.ExtensionManager.GetServices<IPreprocessorExtension>();
            context.Platform = this.Platform;
            context.IncludeSearchPaths = includeSearchPaths;
            context.IntermediateFolder = this.IntermediateFolder;
            context.OutputPath = this.OutputPath;
            context.SourcePath = sourcePath;
            context.Variables = preprocessorVariables;
            context.CancellationToken = cancellationToken;

            IPreprocessResult result = null;
            try
            {
                var preprocessor = this.ServiceProvider.GetService<IPreprocessor>();
                result = preprocessor.Preprocess(context);
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
            }

            return result?.Document;
        }

        private static string DefaultExtensionForSectionType(SectionType sectionType)
        {
            switch (sectionType)
            {
                case SectionType.Bundle:
                    return ".exe";
                case SectionType.Module:
                    return ".msm";
                case SectionType.Product:
                    return ".msi";
                case SectionType.PatchCreation:
                    return ".pcp";
                case SectionType.Patch:
                    return ".msp";
                case SectionType.Fragment:
                case SectionType.Unknown:
                default:
                    return ".wix";
            }
        }

        private static string DefaultExtensionForOutputType(OutputType outputType)
        {
            switch (outputType)
            {
                case OutputType.Bundle:
                    return ".exe";
                case OutputType.Library:
                    return ".wixlib";
                case OutputType.Module:
                    return ".msm";
                case OutputType.Patch:
                    return ".msp";
                case OutputType.PatchCreation:
                    return ".pcp";
                case OutputType.Product:
                    return ".msi";
                case OutputType.Transform:
                    return ".mst";
                case OutputType.IntermediatePostLink:
                    return ".wixipl";
                case OutputType.Unknown:
                default:
                    return ".wix";
            }
        }

        private class CommandLine
        {
            private static readonly char[] BindPathSplit = { '=' };

            public bool BindFiles { get; private set; }

            public List<IBindPath> BindPaths { get; } = new List<IBindPath>();

            public string CabCachePath { get; private set; }

            public List<string> Cultures { get; } = new List<string>();

            public List<string> Defines { get; } = new List<string>();

            public List<string> IncludeSearchPaths { get; } = new List<string>();

            public List<string> LocalizationFilePaths { get; } = new List<string>();

            public List<string> LibraryFilePaths { get; } = new List<string>();

            public List<string> SourceFilePaths { get; } = new List<string>();

            public List<string> UnevaluatedInputFilePaths { get; } = new List<string>();

            public Platform Platform { get; private set; }

            public string PdbFile { get; private set; }

            public PdbType PdbType { get; private set; }

            public bool ShowLogo { get; set; }

            public bool ShowHelp { get; set; }

            public string IntermediateFolder { get; private set; }

            public string OutputFile { get; private set; }

            public string OutputType { get; private set; }

            public CompressionLevel? DefaultCompressionLevel { get; private set; }

            public string TrackingFile { get; private set; }

            public bool ResetAcls { get; set; }

            public CommandLine(IServiceProvider serviceProvider, IMessaging messaging)
            {
                this.ServiceProvider = serviceProvider;
                this.Messaging = messaging;
            }

            private IServiceProvider ServiceProvider { get; }

            private IMessaging Messaging { get; }

            public bool TryParseArgument(string arg, ICommandLineParser parser)
            {
                if (parser.IsSwitch(arg))
                {
                    var parameter = arg.Substring(1).ToLowerInvariant();
                    switch (parameter)
                    {
                        case "arch":
                        case "platform":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (Enum.TryParse(value, true, out Platform platform))
                            {
                                this.Platform = platform;
                                return true;
                            }
                            break;
                        }

                        case "bf":
                        case "bindfiles":
                            this.BindFiles = true;
                            return true;

                        case "bindpath":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (value != null && this.TryParseBindPath(value, out var bindPath))
                            {
                                this.BindPaths.Add(bindPath);
                                return true;
                            }
                            return false;
                        }

                        case "cc":
                            this.CabCachePath = parser.GetNextArgumentOrError(arg);
                            return true;

                        case "culture":
                            parser.GetNextArgumentOrError(arg, this.Cultures);
                            return true;

                        case "trackingfile":
                            this.TrackingFile = parser.GetNextArgumentAsFilePathOrError(arg);
                            return true;

                        case "d":
                        case "define":
                            parser.GetNextArgumentOrError(arg, this.Defines);
                            return true;

                        case "dcl":
                        case "defaultcompressionlevel":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (Enum.TryParse(value, true, out CompressionLevel compressionLevel))
                            {
                                this.DefaultCompressionLevel = compressionLevel;
                                return true;
                            }
                            return false;
                        }

                        case "i":
                        case "includepath":
                            parser.GetNextArgumentOrError(arg, this.IncludeSearchPaths);
                            return true;

                        case "intermediatefolder":
                            this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(arg);
                            return true;

                        case "loc":
                            parser.GetNextArgumentAsFilePathOrError(arg, "localization files", this.LocalizationFilePaths);
                            return true;

                        case "lib":
                            parser.GetNextArgumentAsFilePathOrError(arg, "library files", this.LibraryFilePaths);
                            return true;

                        case "src":
                            parser.GetNextArgumentAsFilePathOrError(arg, "source code", this.SourceFilePaths);
                            return true;

                        case "o":
                        case "out":
                            this.OutputFile = parser.GetNextArgumentAsFilePathOrError(arg);
                            return true;

                        case "outputtype":
                            this.OutputType = parser.GetNextArgumentOrError(arg);
                            return true;

                        case "pdb":
                            this.PdbFile = parser.GetNextArgumentAsFilePathOrError(arg);
                            return true;

                        case "pdbtype":
                            {
                                var value = parser.GetNextArgumentOrError(arg);
                                if (Enum.TryParse(value, true, out PdbType pdbType))
                                {
                                    this.PdbType = pdbType;
                                    return true;
                                }
                                return false;
                            }

                        case "resetacls":
                            this.ResetAcls = true;
                            return true;
                    }

                    return false;
                }
                else
                {
                    parser.GetArgumentAsFilePathOrError(arg, "input file", this.UnevaluatedInputFilePaths);
                    return true;
                }
            }

            public string CalculateIntermedateFolder()
            {
                var intermediateFolder = this.IntermediateFolder;

                if (String.IsNullOrEmpty(intermediateFolder))
                {
                    intermediateFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                }

                return intermediateFolder;
            }

            public OutputType CalculateOutputType()
            {
                if (String.IsNullOrEmpty(this.OutputType))
                {
                    this.OutputType = Path.GetExtension(this.OutputFile);
                }

                switch (this.OutputType?.ToLowerInvariant())
                {
                    case "bundle":
                    case ".exe":
                        return Data.OutputType.Bundle;

                    case "library":
                    case ".wixlib":
                        return Data.OutputType.Library;

                    case "module":
                    case ".msm":
                        return Data.OutputType.Module;

                    case "patch":
                    case ".msp":
                        return Data.OutputType.Patch;

                    case ".pcp":
                        return Data.OutputType.PatchCreation;

                    case "product":
                    case "package":
                    case ".msi":
                        return Data.OutputType.Product;

                    case "transform":
                    case ".mst":
                        return Data.OutputType.Transform;

                    case "intermediatepostlink":
                    case ".wixipl":
                        return Data.OutputType.IntermediatePostLink;
                }

                return Data.OutputType.Unknown;
            }

            public IReadOnlyList<string> CalculateFilterCultures()
            {
                var result = new List<string>();

                if (this.Cultures == null)
                {
                }
                else if (this.Cultures.Count == 1 && this.Cultures[0].Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    // When null is used treat it as if cultures wasn't specified. This is
                    // needed for batching in the MSBuild task since MSBuild doesn't support
                    // empty items.
                }
                else
                {
                    foreach (var culture in this.Cultures)
                    {
                        // Neutral is different from null. For neutral we still want to do culture filtering.
                        // Set the culture to the empty string = identifier for the invariant culture.
                        var filter = (culture.Equals("neutral", StringComparison.OrdinalIgnoreCase)) ? String.Empty : culture;
                        result.Add(filter);
                    }
                }

                return result;
            }

            public IDictionary<string, string> CalculatePreprocessorVariables()
            {
                var variables = new Dictionary<string, string>();

                foreach (var pair in this.Defines)
                {
                    var value = pair.Split(new[] { '=' }, 2);

                    if (variables.ContainsKey(value[0]))
                    {
                        this.Messaging.Write(ErrorMessages.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], variables[value[0]]));
                        continue;
                    }

                    variables.Add(value[0], (1 == value.Length) ? String.Empty : value[1]);
                }

                return variables;
            }

            public InputsAndOutputs CalculateInputsAndOutputs(ISymbolDefinitionCreator creator)
            {
                var codePaths = new List<string>(this.SourceFilePaths);
                var localizationPaths = new List<string>(this.LocalizationFilePaths);
                var libraryPaths = new List<string>(this.LibraryFilePaths);
                var wixipls = new List<Intermediate>();
                string lastWixiplPath = null;

                var outputPath = this.OutputFile;
                var outputType = this.CalculateOutputType();

                foreach (var path in this.UnevaluatedInputFilePaths)
                {
                    var extension = Path.GetExtension(path);

                    if (".wxs".Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        codePaths.Add(path);
                    }
                    else if (".wxl".Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        localizationPaths.Add(path);
                    }
                    else if (".wixlib".Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        libraryPaths.Add(path);
                    }
                    else
                    {
                        try
                        {
                            // Try to load the file as an intermediate to determine whether it is a
                            // .wixipl or a .wixlib.
                            var intermediate = Intermediate.Load(path, creator);

                            if (intermediate.HasLevel(IntermediateLevels.Linked))
                            {
                                wixipls.Add(intermediate);
                                lastWixiplPath = path;
                            }
                            else
                            {
                                libraryPaths.Add(path);
                            }
                        }
                        catch (WixException)
                        {
                            // We'll assume anything that isn't a valid intermediate is source code to compile.
                            codePaths.Add(path);
                        }
                    }
                }

                if (wixipls.Count > 0)
                {
                    if (wixipls.Count > 1 || codePaths.Count > 0 || libraryPaths.Count > 0)
                    {
                        this.Messaging.Write(ErrorMessages.WixiplSourceFileIsExclusive());
                    }
                }
                else if (codePaths.Count == 0 && libraryPaths.Count == 0)
                {
                    this.Messaging.Write(ErrorMessages.NoSourceFiles());
                }

                if (!this.Messaging.EncounteredError && String.IsNullOrEmpty(outputPath))
                {
                    var singleInputPath = codePaths.Count == 1 ? codePaths[0] : lastWixiplPath;

                    if (String.IsNullOrEmpty(singleInputPath))
                    {
                        this.Messaging.Write(ErrorMessages.MustSpecifyOutputWithMoreThanOneInput());
                    }
                    else
                    {
                        // If output type is unknown, the extension will be replaced with the right default based on output type.
                        outputPath = Path.ChangeExtension(singleInputPath, DefaultExtensionForOutputType(outputType));
                    }
                }

                var pdbPath = this.PdbType == PdbType.None ? null : this.PdbFile ?? Path.ChangeExtension(outputPath ?? "error.above", ".wixpdb");

                return new InputsAndOutputs(codePaths, localizationPaths, libraryPaths, wixipls, outputPath, outputType, pdbPath, this.PdbType);
            }

            private bool TryParseBindPath(string bindPath, out IBindPath bp)
            {
                var namedPath = bindPath.Split(BindPathSplit, 2);

                bp = this.ServiceProvider.GetService<IBindPath>();

                if (1 == namedPath.Length)
                {
                    bp.Path = namedPath[0];
                }
                else
                {
                    bp.Name = namedPath[0];
                    bp.Path = namedPath[1];
                }

                if (File.Exists(bp.Path))
                {
                    this.Messaging.Write(ErrorMessages.ExpectedDirectoryGotFile("-bindpath", bp.Path));
                    return false;
                }

                return true;
            }
        }

        private class InputsAndOutputs
        {
            public InputsAndOutputs(IReadOnlyCollection<string> sourcePaths, IReadOnlyCollection<string> localizationPaths, IReadOnlyCollection<string> libraryPaths, IReadOnlyCollection<Intermediate> wixipls, string outputPath, OutputType outputType, string pdbPath, PdbType pdbType)
            {
                this.SourcePaths = sourcePaths;
                this.LocalizationPaths = localizationPaths;
                this.LibraryPaths = libraryPaths;
                this.Wixipls = wixipls;
                this.OutputPath = outputPath;
                this.OutputType = outputType;
                this.PdbPath = pdbPath;
                this.PdbType = pdbType;
            }

            public IReadOnlyCollection<string> SourcePaths { get; }

            public IReadOnlyCollection<string> LocalizationPaths { get; }

            public IReadOnlyCollection<string> LibraryPaths { get; }

            public IReadOnlyCollection<Intermediate> Wixipls { get; }

            public string OutputPath { get; set; }

            public OutputType OutputType { get; }

            public string PdbPath { get; }

            public PdbType PdbType { get; }
        }
    }
}
