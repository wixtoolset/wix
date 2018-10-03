// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal enum Commands
    {
        Unknown,
        Build,
        Preprocess,
        Compile,
        Link,
        Bind,
    }

    internal class CommandLineParser : ICommandLineParser
    {
        private static readonly char[] BindPathSplit = { '=' };

        public CommandLineParser(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; set; }

        public IExtensionManager ExtensionManager { get; set; }

        public ICommandLineArguments Arguments { get; set; }

        public static string ExpectedArgument { get; } = "expected argument";

        public string ActiveCommand { get; private set; }

        public bool ShowHelp { get; private set; }

        public ICommandLineCommand ParseStandardCommandLine()
        {
            var context = this.ServiceProvider.GetService<ICommandLineContext>();
            context.ExtensionManager = this.ExtensionManager ?? this.ServiceProvider.GetService<IExtensionManager>();
            context.Arguments = this.Arguments;

            var next = String.Empty;

            var command = Commands.Unknown;
            var showLogo = true;
            var showVersion = false;
            var outputFolder = String.Empty;
            var outputFile = String.Empty;
            var outputType = String.Empty;
            var platformType = String.Empty;
            var verbose = false;
            var files = new List<string>();
            var defines = new List<string>();
            var includePaths = new List<string>();
            var locFiles = new List<string>();
            var libraryFiles = new List<string>();
            var suppressedWarnings = new List<int>();

            var bindFiles = false;
            var bindPaths = new List<string>();

            var intermediateFolder = String.Empty;

            var cabCachePath = String.Empty;
            var cultures = new List<string>();
            var contentsFile = String.Empty;
            var outputsFile = String.Empty;
            var builtOutputsFile = String.Empty;

            this.Parse(context, (cmdline, arg) => Enum.TryParse(arg, true, out command), (cmdline, parser, arg) =>
            {
                if (parser.IsSwitch(arg))
                {
                    var parameter = arg.Substring(1);
                    switch (parameter.ToLowerInvariant())
                    {
                    case "?":
                    case "h":
                    case "help":
                        cmdline.ShowHelp = true;
                        return true;

                    case "arch":
                    case "platform":
                        platformType = parser.GetNextArgumentOrError(arg);
                        return true;

                    case "bindfiles":
                        bindFiles = true;
                        return true;

                    case "bindpath":
                        parser.GetNextArgumentOrError(arg, bindPaths);
                        return true;

                    case "cc":
                        cabCachePath = parser.GetNextArgumentOrError(arg);
                        return true;

                    case "culture":
                        parser.GetNextArgumentOrError(arg, cultures);
                        return true;
                    case "contentsfile":
                        contentsFile = parser.GetNextArgumentAsFilePathOrError(arg);
                        return true;
                    case "outputsfile":
                        outputsFile = parser.GetNextArgumentAsFilePathOrError(arg);
                        return true;
                    case "builtoutputsfile":
                        builtOutputsFile = parser.GetNextArgumentAsFilePathOrError(arg);
                        return true;

                    case "d":
                    case "define":
                        parser.GetNextArgumentOrError(arg, defines);
                        return true;

                    case "i":
                    case "includepath":
                        parser.GetNextArgumentOrError(arg, includePaths);
                        return true;

                    case "intermediatefolder":
                        intermediateFolder = parser.GetNextArgumentAsDirectoryOrError(arg);
                        return true;

                    case "loc":
                        parser.GetNextArgumentAsFilePathOrError(arg, "localization files", locFiles);
                        return true;

                    case "lib":
                        parser.GetNextArgumentAsFilePathOrError(arg, "library files", libraryFiles);
                        return true;

                    case "o":
                    case "out":
                        outputFile = parser.GetNextArgumentAsFilePathOrError(arg);
                        return true;

                    case "outputtype":
                        outputType = parser.GetNextArgumentOrError(arg);
                        return true;

                    case "nologo":
                        showLogo = false;
                        return true;

                    case "v":
                    case "verbose":
                        verbose = true;
                        return true;

                    case "version":
                    case "-version":
                        showVersion = true;
                        return true;

                    case "sval":
                        // todo: implement
                        return true;

                    case "sw":
                    case "suppresswarning":
                        var warning = parser.GetNextArgumentOrError(arg);
                        if (!String.IsNullOrEmpty(warning))
                        {
                            var warningNumber = Convert.ToInt32(warning);
                            this.Messaging.SuppressWarningMessage(warningNumber);
                        }
                        return true;
                    }

                    return false;
                }
                else
                {
                    parser.GetArgumentAsFilePathOrError(arg, "source code", files);
                    return true;
                }
            });

            this.Messaging.ShowVerboseMessages = verbose;

            if (showVersion)
            {
                return new VersionCommand();
            }

            if (showLogo)
            {
                AppCommon.DisplayToolHeader();
            }

            if (this.ShowHelp)
            {
                return new HelpCommand(command);
            }

            switch (command)
            {
            case Commands.Build:
            {
                var sourceFiles = GatherSourceFiles(files, outputFolder);
                var variables = this.GatherPreprocessorVariables(defines);
                var bindPathList = this.GatherBindPaths(bindPaths);
                var filterCultures = CalculateFilterCultures(cultures);
                var type = CalculateOutputType(outputType, outputFile);
                var platform = CalculatePlatform(platformType);
                return new BuildCommand(this.ServiceProvider, sourceFiles, variables, locFiles, libraryFiles, filterCultures, outputFile, type, platform, cabCachePath, bindFiles, bindPathList, includePaths, intermediateFolder, contentsFile, outputsFile, builtOutputsFile);
            }

            case Commands.Compile:
            {
                var sourceFiles = GatherSourceFiles(files, outputFolder);
                var variables = this.GatherPreprocessorVariables(defines);
                var platform = CalculatePlatform(platformType);
                return new CompileCommand(this.ServiceProvider, sourceFiles, variables, platform);
            }
            }

            return null;
        }

        private static IEnumerable<string> CalculateFilterCultures(List<string> cultures)
        {
            var result = new List<string>();

            if (cultures == null)
            {
            }
            else if (cultures.Count == 1 && cultures[0].Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                // When null is used treat it as if cultures wasn't specified. This is
                // needed for batching in the MSBuild task since MSBuild doesn't support
                // empty items.
            }
            else
            {
                foreach (var culture in cultures)
                {
                    // Neutral is different from null. For neutral we still want to do culture filtering.
                    // Set the culture to the empty string = identifier for the invariant culture.
                    var filter = (culture.Equals("neutral", StringComparison.OrdinalIgnoreCase)) ? String.Empty : culture;
                    result.Add(filter);
                }
            }

            return result;
        }

        private static OutputType CalculateOutputType(string outputType, string outputFile)
        {
            if (String.IsNullOrEmpty(outputType))
            {
                outputType = Path.GetExtension(outputFile);
            }

            switch (outputType.ToLowerInvariant())
            {
            case "bundle":
            case ".exe":
                return OutputType.Bundle;

            case "library":
            case ".wixlib":
                return OutputType.Library;

            case "module":
            case ".msm":
                return OutputType.Module;

            case "patch":
            case ".msp":
                return OutputType.Patch;

            case ".pcp":
                return OutputType.PatchCreation;

            case "product":
            case "package":
            case ".msi":
                return OutputType.Product;

            case "transform":
            case ".mst":
                return OutputType.Transform;

            case "intermediatepostlink":
            case ".wixipl":
                return OutputType.IntermediatePostLink;
            }

            return OutputType.Unknown;
        }

        private static Platform CalculatePlatform(string platformType)
        {
            return Enum.TryParse(platformType, true, out Platform platform) ? platform : Platform.X86;
        }

        private ICommandLineParser Parse(ICommandLineContext context, Func<CommandLineParser, string, bool> parseCommand, Func<CommandLineParser, IParseCommandLine, string, bool> parseArgument)
        {
            var extensions = this.ExtensionManager.Create<IExtensionCommandLine>();

            foreach (var extension in extensions)
            {
                extension.PreParse(context);
            }

            var parser = context.Arguments.Parse();

            while (!this.ShowHelp &&
                   String.IsNullOrEmpty(parser.ErrorArgument) &&
                   parser.TryGetNextSwitchOrArgument(out var arg))
            {
                if (String.IsNullOrWhiteSpace(arg)) // skip blank arguments.
                {
                    continue;
                }

                if (parser.IsSwitch(arg))
                {
                    if (!parseArgument(this, parser, arg) &&
                        !this.TryParseCommandLineArgumentWithExtension(arg, parser, extensions))
                    {
                        parser.ErrorArgument = arg;
                    }
                }
                else if (String.IsNullOrEmpty(this.ActiveCommand) && parseCommand != null) // First non-switch must be the command, if commands are supported.
                {
                    if (parseCommand(this, arg))
                    {
                        this.ActiveCommand = arg;
                    }
                    else
                    {
                        parser.ErrorArgument = arg;
                    }
                }
                else if (!this.TryParseCommandLineArgumentWithExtension(arg, parser, extensions) &&
                         !parseArgument(this, parser, arg))
                {
                    parser.ErrorArgument = arg;
                }
            }

            foreach (var extension in extensions)
            {
                extension.PostParse();
            }

            return this;
        }

        private static IEnumerable<SourceFile> GatherSourceFiles(IEnumerable<string> sourceFiles, string intermediateDirectory)
        {
            var files = new List<SourceFile>();

            foreach (var item in sourceFiles)
            {
                var sourcePath = item;
                var outputPath = Path.Combine(intermediateDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".wir");

                files.Add(new SourceFile(sourcePath, outputPath));
            }

            return files;
        }

        private IDictionary<string, string> GatherPreprocessorVariables(IEnumerable<string> defineConstants)
        {
            var variables = new Dictionary<string, string>();

            foreach (var pair in defineConstants)
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

        private IEnumerable<BindPath> GatherBindPaths(IEnumerable<string> bindPaths)
        {
            var result = new List<BindPath>();

            foreach (var bindPath in bindPaths)
            {
                var bp = ParseBindPath(bindPath);

                if (File.Exists(bp.Path))
                {
                    this.Messaging.Write(ErrorMessages.ExpectedDirectoryGotFile("-bindpath", bp.Path));
                }
                else
                {
                    result.Add(bp);
                }
            }

            return result;
        }

        private bool TryParseCommandLineArgumentWithExtension(string arg, IParseCommandLine parse, IEnumerable<IExtensionCommandLine> extensions)
        {
            foreach (var extension in extensions)
            {
                if (extension.TryParseArgument(parse, arg))
                {
                    return true;
                }
            }

            return false;
        }

        public static BindPath ParseBindPath(string bindPath)
        {
            var namedPath = bindPath.Split(BindPathSplit, 2);
            return (1 == namedPath.Length) ? new BindPath(namedPath[0]) : new BindPath(namedPath[0], namedPath[1]);
        }
    }
}
