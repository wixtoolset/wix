// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixToolset.Data;
    using WixToolset.Extensibility;
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

    internal class CommandLineParser : ICommandLine, IParseCommandLine
    {
        private IServiceProvider ServiceProvider { get; set; }

        private IMessaging Messaging { get; set; }

        public static string ExpectedArgument { get; } = "expected argument";

        public string ActiveCommand { get; private set; }

        public string[] OriginalArguments { get; private set; }

        public Queue<string> RemainingArguments { get; } = new Queue<string>();

        public IExtensionManager ExtensionManager { get; private set; }

        public string ErrorArgument { get; set; }

        public bool ShowHelp { get; set; }

        public ICommandLineCommand ParseStandardCommandLine(ICommandLineContext context)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.Messaging ?? this.ServiceProvider.GetService<IMessaging>();

            this.ExtensionManager = context.ExtensionManager ?? this.ServiceProvider.GetService<IExtensionManager>();

            var args = context.ParsedArguments ?? Array.Empty<string>();

            if (!String.IsNullOrEmpty(context.Arguments))
            {
                args = CommandLineParser.ParseArgumentsToArray(context.Arguments).Concat(args).ToArray();
            }

            return this.ParseStandardCommandLine(context, args);
        }

        private ICommandLineCommand ParseStandardCommandLine(ICommandLineContext context, string[] args)
        {
            var next = String.Empty;

            var command = Commands.Unknown;
            var showLogo = true;
            var showVersion = false;
            var outputFolder = String.Empty;
            var outputFile = String.Empty;
            var outputType = String.Empty;
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

            this.Parse(context, args, (cmdline, arg) => Enum.TryParse(arg, true, out command), (cmdline, arg) =>
            {
                if (cmdline.IsSwitch(arg))
                {
                    var parameter = arg.Substring(1);
                    switch (parameter.ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            cmdline.ShowHelp = true;
                            return true;

                        case "bindfiles":
                            bindFiles = true;
                            return true;

                        case "bindpath":
                            cmdline.GetNextArgumentOrError(bindPaths);
                            return true;

                        case "cc":
                            cmdline.GetNextArgumentOrError(ref cabCachePath);
                            return true;

                        case "cultures":
                            cmdline.GetNextArgumentOrError(cultures);
                            return true;
                        case "contentsfile":
                            cmdline.GetNextArgumentOrError(ref contentsFile);
                            return true;
                        case "outputsfile":
                            cmdline.GetNextArgumentOrError(ref outputsFile);
                            return true;
                        case "builtoutputsfile":
                            cmdline.GetNextArgumentOrError(ref builtOutputsFile);
                            return true;

                        case "d":
                        case "define":
                            cmdline.GetNextArgumentOrError(defines);
                            return true;

                        case "i":
                        case "includepath":
                            cmdline.GetNextArgumentOrError(includePaths);
                            return true;

                        case "intermediatefolder":
                            cmdline.GetNextArgumentOrError(ref intermediateFolder);
                            return true;

                        case "loc":
                            cmdline.GetNextArgumentAsFilePathOrError(locFiles, "localization files");
                            return true;

                        case "lib":
                            cmdline.GetNextArgumentAsFilePathOrError(libraryFiles, "library files");
                            return true;

                        case "o":
                        case "out":
                            cmdline.GetNextArgumentOrError(ref outputFile);
                            return true;

                        case "outputtype":
                            cmdline.GetNextArgumentOrError(ref outputType);
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
                    }

                    return false;
                }
                else
                {
                    files.AddRange(CommandLineHelper.GetFiles(arg, "source code"));
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
                        var type = CalculateOutputType(outputType, outputFile);
                        return new BuildCommand(this.ServiceProvider, sourceFiles, variables, locFiles, libraryFiles, outputFile, type, cabCachePath, cultures, bindFiles, bindPathList, includePaths, intermediateFolder, contentsFile, outputsFile, builtOutputsFile);
                    }

                case Commands.Compile:
                    {
                        var sourceFiles = GatherSourceFiles(files, outputFolder);
                        var variables = GatherPreprocessorVariables(defines);
                        return new CompileCommand(this.ServiceProvider, sourceFiles, variables);
                    }
            }

            return null;
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

#if UNUSED
        private static CommandLine Parse(string commandLineString, Func<CommandLine, string, bool> parseArgument)
        {
            var arguments = CommandLine.ParseArgumentsToArray(commandLineString).ToArray();

            return CommandLine.Parse(arguments, null, parseArgument);
        }

        private static CommandLine Parse(string[] commandLineArguments, Func<CommandLine, string, bool> parseArgument)
        {
            return CommandLine.Parse(commandLineArguments, null, parseArgument);
        }
#endif

        private ICommandLine Parse(ICommandLineContext context, string[] commandLineArguments, Func<CommandLineParser, string, bool> parseCommand, Func<CommandLineParser, string, bool> parseArgument)
        {
            this.FlattenArgumentsWithResponseFilesIntoOriginalArguments(commandLineArguments);

            this.QueueArgumentsAndLoadExtensions(this.OriginalArguments);

            this.ProcessRemainingArguments(context, parseArgument, parseCommand);

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
                string[] value = pair.Split(new[] { '=' }, 2);

                if (variables.ContainsKey(value[0]))
                {
                    this.Messaging.Write(ErrorMessages.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], variables[value[0]]));
                    continue;
                }

                variables.Add(value[0], (1 == value.Length) ? String.Empty : value[1]);
            }

            return variables;
        }

        private  IEnumerable<BindPath> GatherBindPaths(IEnumerable<string> bindPaths)
        {
            var result = new List<BindPath>();

            foreach (var bindPath in bindPaths)
            {
                var bp = BindPath.Parse(bindPath);

                if (Directory.Exists(bp.Path))
                {
                    result.Add(bp);
                }
                else if (File.Exists(bp.Path))
                {
                    this.Messaging.Write(ErrorMessages.ExpectedDirectoryGotFile("-bindpath", bp.Path));
                }
            }

            return result;
        }

        /// <summary>
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid switch exists there, false if not.</returns>
        public bool IsSwitch(string arg)
        {
            return arg != null && arg.Length > 1 && ('/' == arg[0] || '-' == arg[0]);
        }

        /// <summary>
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid switch exists there, false if not.</returns>
        public bool IsSwitchAt(IEnumerable<string> args, int index)
        {
            var arg = args.ElementAtOrDefault(index);
            return IsSwitch(arg);
        }

        public void GetNextArgumentOrError(ref string arg)
        {
            this.TryGetNextArgumentOrError(out arg);
        }

        public void GetNextArgumentOrError(IList<string> args)
        {
            if (this.TryGetNextArgumentOrError(out var arg))
            {
                args.Add(arg);
            }
        }

        public void GetNextArgumentAsFilePathOrError(IList<string> args, string fileType)
        {
            if (this.TryGetNextArgumentOrError(out var arg))
            {
                foreach (var path in CommandLineHelper.GetFiles(arg, fileType))
                {
                    args.Add(path);
                }
            }
        }

        public bool TryGetNextArgumentOrError(out string arg)
        {
            if (TryDequeue(this.RemainingArguments, out arg) && !this.IsSwitch(arg))
            {
                return true;
            }

            this.ErrorArgument = arg ?? CommandLineParser.ExpectedArgument;

            return false;
        }

        private static bool TryDequeue(Queue<string> q, out string arg)
        {
            if (q.Count > 0)
            {
                arg = q.Dequeue();
                return true;
            }

            arg = null;
            return false;
        }

        private void FlattenArgumentsWithResponseFilesIntoOriginalArguments(string[] commandLineArguments)
        {
            List<string> args = new List<string>();

            foreach (var arg in commandLineArguments)
            {
                if ('@' == arg[0])
                {
                    var responseFileArguments = CommandLineParser.ParseResponseFile(arg.Substring(1));
                    args.AddRange(responseFileArguments);
                }
                else
                {
                    args.Add(arg);
                }
            }

            this.OriginalArguments = args.ToArray();
        }

        private void QueueArgumentsAndLoadExtensions(string[] args)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];

                if ("-ext" == arg || "/ext" == arg)
                {
                    if (!this.IsSwitchAt(args, ++i))
                    {
                        this.ExtensionManager.Load(args[i]);
                    }
                    else
                    {
                        this.ErrorArgument = arg;
                        break;
                    }
                }
                else
                {
                    this.RemainingArguments.Enqueue(arg);
                }
            }
        }

        private void ProcessRemainingArguments(ICommandLineContext context, Func<CommandLineParser, string, bool> parseArgument, Func<CommandLineParser, string, bool> parseCommand)
        {
            var extensions = this.ExtensionManager.Create<IExtensionCommandLine>();

            foreach (var extension in extensions)
            {
                extension.PreParse(context);
            }

            while (!this.ShowHelp &&
                   String.IsNullOrEmpty(this.ErrorArgument) &&
                   TryDequeue(this.RemainingArguments, out var arg))
            {
                if (String.IsNullOrWhiteSpace(arg)) // skip blank arguments.
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    if (!parseArgument(this, arg) &&
                        !this.TryParseCommandLineArgumentWithExtension(arg, extensions))
                    {
                        this.ErrorArgument = arg;
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
                        this.ErrorArgument = arg;
                    }
                }
                else if (!this.TryParseCommandLineArgumentWithExtension(arg, extensions) &&
                         !parseArgument(this, arg))
                {
                    this.ErrorArgument = arg;
                }
            }
        }

        private bool TryParseCommandLineArgumentWithExtension(string arg, IEnumerable<IExtensionCommandLine> extensions)
        {
            foreach (var extension in extensions)
            {
                if (extension.TryParseArgument(this, arg))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> ParseResponseFile(string responseFile)
        {
            string arguments;

            using (StreamReader reader = new StreamReader(responseFile))
            {
                arguments = reader.ReadToEnd();
            }

            return CommandLineParser.ParseArgumentsToArray(arguments);
        }

        private static List<string> ParseArgumentsToArray(string arguments)
        {
            // Scan and parse the arguments string, dividing up the arguments based on whitespace.
            // Unescaped quotes cause whitespace to be ignored, while the quotes themselves are removed.
            // Quotes may begin and end inside arguments; they don't necessarily just surround whole arguments.
            // Escaped quotes and escaped backslashes also need to be unescaped by this process.

            // Collects the final list of arguments to be returned.
            var argsList = new List<string>();

            // True if we are inside an unescaped quote, meaning whitespace should be ignored.
            var insideQuote = false;

            // Index of the start of the current argument substring; either the start of the argument
            // or the start of a quoted or unquoted sequence within it.
            var partStart = 0;

            // The current argument string being built; when completed it will be added to the list.
            var arg = new StringBuilder();

            for (int i = 0; i <= arguments.Length; i++)
            {
                if (i == arguments.Length || (Char.IsWhiteSpace(arguments[i]) && !insideQuote))
                {
                    // Reached a whitespace separator or the end of the string.

                    // Finish building the current argument.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // Skip over the whitespace character.
                    partStart = i + 1;

                    // Add the argument to the list if it's not empty.
                    if (arg.Length > 0)
                    {
                        argsList.Add(CommandLineParser.ExpandEnvironmentVariables(arg.ToString()));
                        arg.Length = 0;
                    }
                }
                else if (i > partStart && arguments[i - 1] == '\\')
                {
                    // Check the character following an unprocessed backslash.
                    // Unescape quotes, and backslashes followed by a quote.
                    if (arguments[i] == '"' || (arguments[i] == '\\' && arguments.Length > i + 1 && arguments[i + 1] == '"'))
                    {
                        // Unescape the quote or backslash by skipping the preceeding backslash.
                        arg.Append(arguments.Substring(partStart, i - 1 - partStart));
                        arg.Append(arguments[i]);
                        partStart = i + 1;
                    }
                }
                else if (arguments[i] == '"')
                {
                    // Add the quoted or unquoted section to the argument string.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // And skip over the quote character.
                    partStart = i + 1;

                    insideQuote = !insideQuote;
                }
            }

            return argsList;
        }

        private static string ExpandEnvironmentVariables(string arguments)
        {
            var id = Environment.GetEnvironmentVariables();

            var regex = new Regex("(?<=\\%)(?:[\\w\\.]+)(?=\\%)");
            MatchCollection matches = regex.Matches(arguments);

            string value = String.Empty;
            for (int i = 0; i <= (matches.Count - 1); i++)
            {
                try
                {
                    var key = matches[i].Value;
                    regex = new Regex(String.Concat("(?i)(?:\\%)(?:", key, ")(?:\\%)"));
                    value = id[key].ToString();
                    arguments = regex.Replace(arguments, value);
                }
                catch (NullReferenceException)
                {
                    // Collapse unresolved environment variables.
                    arguments = regex.Replace(arguments, value);
                }
            }

            return arguments;
        }
    }
}
