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

    internal class BuildCommand : BaseCommandLineCommand
    {
        private readonly CommandLine commandLine;

        public BuildCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.commandLine = new CommandLine(this.ServiceProvider, this.Messaging);
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IExtensionManager ExtensionManager { get; }

        private string IntermediateFolder { get; set; }

        private string OutputPath { get; set; }

        private Platform Platform { get; set; }

        private CompressionLevel? DefaultCompressionLevel { get; set; }

        private string TrackingFile { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("", "build [options] sourcefile [sourcefile ...] -out output.ext", new[]
            {
                new CommandLineHelpSwitch("-arch", "Set the architecture of the output."),
                new CommandLineHelpSwitch("-bindfiles", "-bf", "Bind files into an output .wixlib. Ignored if not building a .wixlib."),
                new CommandLineHelpSwitch("-bindpath", "-b", "Bind path to search for content files."),
                new CommandLineHelpSwitch("-bindpath:target", "-bt", "Bind path to search for target package's content files. Only used when building a patch."),
                new CommandLineHelpSwitch("-bindpath:update", "-bu", "Bind path to search for update package's content files. Only used when building a patch."),
                new CommandLineHelpSwitch("-cabcache", "-cc", "Set a folder to cache cabinets across builds."),
                new CommandLineHelpSwitch("-cabthreads", "-ct", "Override the number of threads used to create cabinets."),
                new CommandLineHelpSwitch("-culture", "Adds a culture to filter localization files."),
                new CommandLineHelpSwitch("-define", "-d", "Sets a preprocessor variable."),
                new CommandLineHelpSwitch("-defaultcompressionlevel", "-dcl", "Default compression level; see Compression levels below."),
                new CommandLineHelpSwitch("-include", "-i", "Folder to search for include files."),
                new CommandLineHelpSwitch("-intermediatefolder", "Optional working folder. If not specified a folder in %TMP% will be created."),
                new CommandLineHelpSwitch("-loc", "Localization file to use in the build. By default, .wxl files are recognized as localization."),
                new CommandLineHelpSwitch("-lib", "Library file to use in the build. By default, .wixlib files are recognized as libraries."),
                new CommandLineHelpSwitch("-src", "Source file to use in the build. By default, .wxs files are recognized as source code."),
                new CommandLineHelpSwitch("-out", "-o", "Path to output the build to."),
                new CommandLineHelpSwitch("-outputtype", "Explicitly set the output type if it cannot be determined from the output."),
                new CommandLineHelpSwitch("-pdb", "Optional path to output .wixpdb. Default will write .wixpdb beside output path."),
                new CommandLineHelpSwitch("-pdbtype", "Switch to disable creation of .wixpdb. Types: full or none."),
            })
            {
                Notes = String.Join(Environment.NewLine,
                    "Compression levels:",
                    "  none        Use no compression",
                    "  low         Use low compression",
                    "  medium      Use medium compression",
                    "  high        Use high compression",
                    "  mszip       Use ms-zip compression")
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
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

            this.OptimizePhase(wixobjs, wxls, this.commandLine.BindPaths, this.commandLine.BindVariables, cancellationToken);

            if (inputsOutputs.OutputType == OutputType.Library)
            {
                using (new IntermediateFieldContext("wix.lib"))
                {
                    this.LibraryPhase(wixobjs, wxls, inputsOutputs.LibraryPaths, creator, this.commandLine.BindFiles, this.commandLine.BindPaths, this.commandLine.BindVariables, inputsOutputs.OutputPath, cancellationToken);
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
                                this.BindPhase(wixipl, wxls, filterCultures, this.commandLine.CabCachePath, this.commandLine.CabbingThreadCount, this.commandLine.BindPaths, this.commandLine.BindVariables, inputsOutputs, cancellationToken);
                            }
                        }
                    }
                }
            }

            return Task.FromResult(this.Messaging.LastErrorNumber);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            return this.commandLine.TryParseArgument(argument, parser);
        }

        private IReadOnlyList<Intermediate> CompilePhase(IDictionary<string, string> preprocessorVariables, IEnumerable<string> sourceFiles, IReadOnlyCollection<string> includeSearchPaths, CancellationToken cancellationToken)
        {
            var intermediates = new List<Intermediate>();

            foreach (var sourceFile in sourceFiles)
            {
                var document = this.Preprocess(preprocessorVariables, sourceFile, includeSearchPaths, cancellationToken);

                if (document == null)
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

        private void OptimizePhase(IReadOnlyCollection<Intermediate> intermediates, IReadOnlyCollection<Localization> localizations, IReadOnlyCollection<IBindPath> bindPaths, Dictionary<string, string> bindVariables, CancellationToken cancellationToken)
        {
            var context = this.ServiceProvider.GetService<IOptimizeContext>();
            context.Extensions = this.ExtensionManager.GetServices<IOptimizerExtension>();
            context.IntermediateFolder = this.IntermediateFolder;
            context.BindPaths = bindPaths;
            context.BindVariables = bindVariables;
            context.Platform = this.Platform;
            context.Intermediates = intermediates;
            context.Localizations = localizations;
            context.CancellationToken = cancellationToken;

            var optimizer = this.ServiceProvider.GetService<IOptimizer>();
            optimizer.Optimize(context);
        }

        private void LibraryPhase(IReadOnlyCollection<Intermediate> intermediates, IReadOnlyCollection<Localization> localizations, IEnumerable<string> libraryFiles, ISymbolDefinitionCreator creator, bool bindFiles, IReadOnlyCollection<IBindPath> bindPaths, Dictionary<string, string> bindVariables, string outputPath, CancellationToken cancellationToken)
        {
            var libraries = this.LoadLibraries(libraryFiles, creator);

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            var context = this.ServiceProvider.GetService<ILibraryContext>();
            context.BindFiles = bindFiles;
            context.BindPaths = bindPaths;
            context.BindVariables = bindVariables;
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

        private void BindPhase(Intermediate output, IReadOnlyCollection<Localization> localizations, IReadOnlyCollection<string> filterCultures, string cabCachePath, int cabbingThreadCount, IReadOnlyCollection<IBindPath> bindPaths, Dictionary<string, string> bindVariables, InputsAndOutputs inputsOutputs, CancellationToken cancellationToken)
        {
            IResolveResult resolveResult;
            {
                var context = this.ServiceProvider.GetService<IResolveContext>();
                context.BindPaths = bindPaths;
                context.BindVariables = bindVariables;
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
                    context.BindPaths = bindPaths;
                    context.CabbingThreadCount = cabbingThreadCount;
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
                    context.OutputType = this.commandLine.OutputType;
                    context.PdbType = inputsOutputs.PdbType;
                    context.PdbPath = inputsOutputs.PdbPath;
                    context.CancellationToken = cancellationToken;

                    if (String.IsNullOrEmpty(context.OutputType))
                    {
                        var entrySection = context.IntermediateRepresentation.Sections.First();

                        context.OutputType = entrySection.Type.ToString();
                    }

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

                if (document == null)
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
                case SectionType.Package:
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
                case OutputType.Package:
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

            public Dictionary<string, string> BindVariables { get; } = new Dictionary<string, string>();

            public string CabCachePath { get; private set; }

            public int CabbingThreadCount { get; private set; }

            public List<string> Cultures { get; } = new List<string>();

            public List<string> Defines { get; } = new List<string>();

            public List<string> IncludeSearchPaths { get; } = new List<string>();

            public List<string> LocalizationFilePaths { get; } = new List<string>();

            public List<string> LibraryFilePaths { get; } = new List<string>();

            public List<string> SourceFilePaths { get; } = new List<string>();

            public List<string> UnclassifiedInputFilePaths { get; } = new List<string>();

            public Platform Platform { get; private set; }

            public string PdbFile { get; private set; }

            public PdbType PdbType { get; private set; }

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
                            }
                            else if (!String.IsNullOrEmpty(value))
                            {
                                parser.ReportErrorArgument(arg, ErrorMessages.IllegalCommandLineArgumentValue(arg, value, Enum.GetNames(typeof(Platform)).Select(s => s.ToLowerInvariant())));
                            }

                            return true;
                        }

                        case "bf":
                        case "bindfiles":
                            this.BindFiles = true;
                            return true;

                        case "b":
                        case "bindpath":
                        case "bt":
                        case "bindpath:target":
                        case "bu":
                        case "bindpath:update":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (value != null && this.TryParseBindPath(arg, value, out var bindPath))
                            {
                                if (parameter == "bt" || parameter.EndsWith("target"))
                                {
                                    bindPath.Stage = BindStage.Target;
                                }
                                else if (parameter == "bu" || parameter.EndsWith("update"))
                                {
                                    bindPath.Stage = BindStage.Updated;
                                }

                                this.BindPaths.Add(bindPath);
                            }
                            return true;
                        }

                        case "bv":
                        case "bindvariable":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (value != null && TryParseNameValuePair(value, out var parsedName, out var parsedValue))
                            {
                                if (this.BindVariables.TryGetValue(parsedName, out var collisionValue))
                                {
                                    parser.ReportErrorArgument(arg, LinkerErrors.DuplicateBindPathVariableOnCommandLine(arg, parsedName, parsedValue, collisionValue));
                                }
                                else
                                {
                                    this.BindVariables.Add(parsedName, parsedValue);
                                }
                            }
                            return true;
                        }

                        case "cc":
                        case "cabcache":
                            this.CabCachePath = parser.GetNextArgumentOrError(arg);
                            return true;

                        case "ct":
                        case "cabthreads":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (Int32.TryParse(value, out var cabbingThreads))
                            {
                                this.CabbingThreadCount = cabbingThreads;
                            }
                            else if (!String.IsNullOrEmpty(value))
                            {
                                var processorCount = Environment.ProcessorCount == 0 ? 1 : Environment.ProcessorCount;
                                var range = Enumerable.Range(1, processorCount * 2).Select(i => i.ToString());
                                parser.ReportErrorArgument(arg, ErrorMessages.IllegalCommandLineArgumentValue(arg, value, range));
                            }

                            return true;
                        }

                        case "culture":
                            parser.GetNextArgumentOrError(arg, this.Cultures);
                            return true;

                        case "trackingfile":
                            this.TrackingFile = parser.GetNextArgumentAsFilePathOrError(arg, "tracking file");
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
                            }
                            else if (!String.IsNullOrEmpty(value))
                            {
                                parser.ReportErrorArgument(arg, ErrorMessages.IllegalCommandLineArgumentValue(arg, value, Enum.GetNames(typeof(CompressionLevel)).Select(s => s.ToLowerInvariant())));
                            }

                            return true;
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
                            this.OutputFile = parser.GetNextArgumentAsFilePathOrError(arg, "output file");
                            return true;

                        case "outputtype":
                            this.OutputType = parser.GetNextArgumentOrError(arg);
                            return true;

                        case "pdb":
                            this.PdbFile = parser.GetNextArgumentAsFilePathOrError(arg, "wixpdb file");
                            return true;

                        case "pdbtype":
                        {
                            var value = parser.GetNextArgumentOrError(arg);
                            if (Enum.TryParse(value, true, out PdbType pdbType))
                            {
                                this.PdbType = pdbType;
                            }
                            else if (!String.IsNullOrEmpty(value))
                            {
                                if (value.Equals("embedded", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.Messaging.Write(WarningMessages.UnsupportedCommandLineArgumentValue(arg, value, "full"));

                                    this.PdbType = PdbType.Full;
                                }
                                else
                                {
                                    parser.ReportErrorArgument(arg, ErrorMessages.IllegalCommandLineArgumentValue(arg, value, Enum.GetNames(typeof(PdbType)).Select(s => s.ToLowerInvariant())));
                                }
                            }

                            return true;
                        }

                        case "resetacls":
                            this.ResetAcls = true;
                            return true;
                    }

                    return false;
                }
                else
                {
                    parser.GetArgumentAsFilePathOrError(arg, "input", this.UnclassifiedInputFilePaths);
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
                        return Data.OutputType.Package;

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

                foreach (var path in this.UnclassifiedInputFilePaths)
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

            private bool TryParseBindPath(string argument, string bindPath, out IBindPath bp)
            {
                bp = this.ServiceProvider.GetService<IBindPath>();

                if (TryParseNameValuePair(bindPath, out var name, out var path))
                {
                    bp.Name = name;
                    bp.Path = path;
                }
                else
                {
                    bp.Path = bindPath;
                }

                if (File.Exists(bp.Path))
                {
                    this.Messaging.Write(ErrorMessages.ExpectedDirectoryGotFile(argument, bp.Path));
                    return false;
                }

                return true;
            }

            private static bool TryParseNameValuePair(string nameValuePair, out string key, out string value)
            {
                var split = nameValuePair.Split(BindPathSplit, 2);

                if (1 == split.Length)
                {
                    key = null;
                    value = null;

                    return false;
                }

                key = split[0];
                value = split[1];

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
