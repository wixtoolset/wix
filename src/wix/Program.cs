// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// The main entry point for candle.
    /// </summary>
    public sealed class Program
    {
        //private IEnumerable<IPreprocessorExtension> preprocessorExtensions;
        //private IEnumerable<ICompilerExtension> compilerExtensions;
        //private IEnumerable<IExtensionData> extensionData;

        /// <summary>
        /// The main entry point for candle.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            var command = CommandLine.ParseStandardCommandLine(args);

            return command?.Execute() ?? 1;
        }

#if false
        private static ICommand ParseCommandLine(string[] args)
        {
            var next = String.Empty;

            var command = Commands.Unknown;
            var showLogo = true;
            var showVersion = false;
            var outputFolder = String.Empty;
            var outputFile = String.Empty;
            var sourceFile = String.Empty;
            var verbose = false;
            var files = new List<string>();
            var defines = new List<string>();
            var includePaths = new List<string>();
            var locFiles = new List<string>();
            var suppressedWarnings = new List<int>();

            var cli = CommandLine.Parse(args, (cmdline, arg) => Enum.TryParse(arg, true, out command), (cmdline, arg) =>
            {
                if (cmdline.IsSwitch(arg))
                {
                    var parameter = arg.TrimStart(new[] { '-', '/' });
                    switch (parameter.ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            cmdline.ShowHelp = true;
                            return true;

                        case "d":
                        case "define":
                            cmdline.GetNextArgumentOrError(defines);
                            return true;

                        case "i":
                        case "includepath":
                            cmdline.GetNextArgumentOrError(includePaths);
                            return true;

                        case "loc":
                            cmdline.GetNextArgumentAsFilePathOrError(locFiles, "localization files");
                            return true;

                        case "o":
                        case "out":
                            cmdline.GetNextArgumentOrError(ref outputFile);
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
                    }

                    return false;
                }
                else
                {
                    files.AddRange(cmdline.GetFiles(arg, "source code"));
                    return true;
                }
            });

            if (showVersion)
            {
                return new VersionCommand();
            }

            if (showLogo)
            {
                AppCommon.DisplayToolHeader();
            }

            if (cli.ShowHelp)
            {
                return new HelpCommand(command);
            }

            switch (command)
            {
                case Commands.Build:
                    {
                        var sourceFiles = GatherSourceFiles(files, outputFolder);
                        var variables = GatherPreprocessorVariables(defines);
                        var extensions = cli.ExtensionManager;
                        return new BuildCommand(sourceFiles, variables, locFiles, outputFile);
                    }

                case Commands.Compile:
                    {
                        var sourceFiles = GatherSourceFiles(files, outputFolder);
                        var variables = GatherPreprocessorVariables(defines);
                        return new CompileCommand(sourceFiles, variables);
                    }
            }

            return null;
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

        private static IDictionary<string, string> GatherPreprocessorVariables(IEnumerable<string> defineConstants)
        {
            var variables = new Dictionary<string, string>();

            foreach (var pair in defineConstants)
            {
                string[] value = pair.Split(new[] { '=' }, 2);

                if (variables.ContainsKey(value[0]))
                {
                    Messaging.Instance.OnMessage(WixErrors.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], variables[value[0]]));
                    continue;
                }

                variables.Add(value[0], (1 == value.Length) ? String.Empty : value[1]);
            }

            return variables;
        }
#endif

#if false
        private static ICommand ParseCommandLine2(string[] args)
        {
            var command = Commands.Unknown;

            var nologo = false;
            var outputFolder = String.Empty;
            var outputFile = String.Empty;
            var sourceFile = String.Empty;
            var verbose = false;
            IReadOnlyList<string> files = Array.Empty<string>();
            IReadOnlyList<string> defines = Array.Empty<string>();
            IReadOnlyList<string> includePaths = Array.Empty<string>();
            IReadOnlyList<int> suppressedWarnings = Array.Empty<int>();
            IReadOnlyList<string> locFiles = Array.Empty<string>();

            ArgumentSyntax parsed = null;
            try
            {
                parsed = ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.HandleErrors = false;
                    //syntax.HandleHelp = false;
                    syntax.ErrorOnUnexpectedArguments = false;

                    syntax.DefineCommand("build", ref command, Commands.Build, "Build to final output");
                    syntax.DefineOptionList("d|D|define", ref defines, "Preprocessor name value pairs");
                    syntax.DefineOptionList("I|includePath", ref includePaths, "Include search paths");
                    syntax.DefineOption("nologo", ref nologo, false, "Do not display logo");
                    syntax.DefineOption("o|out", ref outputFile, "Output file");
                    syntax.DefineOptionList("sw", ref suppressedWarnings, false, "Do not display logo");
                    syntax.DefineOption("v|verbose", ref verbose, false, "Display verbose messages");
                    syntax.DefineOptionList("l|loc", ref locFiles, "Localization files to load (.wxl)");
                    syntax.DefineParameterList("files", ref files, "Source files to compile (.wxs)");

                    syntax.DefineCommand("preprocess", ref command, Commands.Preprocess, "Preprocess a source files");
                    syntax.DefineOptionList("d|D|define", ref defines, "Preprocessor name value pairs");
                    syntax.DefineOptionList("I|includePath", ref includePaths, "Include search paths");
                    syntax.DefineOption("nologo", ref nologo, false, "Do not display logo");
                    syntax.DefineOption("o|out", ref outputFile, "Output file");
                    syntax.DefineParameter("file", ref sourceFile, "File to process");

                    syntax.DefineCommand("compile", ref command, Commands.Compile, "Compile source files");
                    syntax.DefineOptionList("I|includePath", ref includePaths, "Include search paths");
                    syntax.DefineOption("nologo", ref nologo, false, "Do not display logo");
                    syntax.DefineOption("o|out", ref outputFolder, "Output folder");
                    syntax.DefineOptionList("sw", ref suppressedWarnings, false, "Do not display logo");
                    syntax.DefineOption("v|verbose", ref verbose, false, "Display verbose messages");
                    syntax.DefineParameterList("files", ref files, "Source files to compile (.wxs)");

                    syntax.DefineCommand("link", ref command, Commands.Link, "Link intermediate files");
                    syntax.DefineOption("nologo", ref nologo, "Do not display logo");
                    syntax.DefineOption("o|out", ref outputFile, "Output intermediate file (.wir)");
                    syntax.DefineParameterList("files", ref files, "Intermediate files to link (.wir)");

                    syntax.DefineCommand("bind", ref command, Commands.Bind, "Bind to final output");
                    syntax.DefineOption("nologo", ref nologo, false, "Do not display logo");
                    syntax.DefineOption("o|out", ref outputFile, "Output file");
                    syntax.DefineParameterList("files", ref files, "Intermediate files to bind (.wir)");

                    syntax.DefineCommand("version", ref command, Commands.Version, "Display version information");
                });

                if (IsHelpRequested(parsed))
                {
                    var width = Console.WindowWidth - 2;
                    var text = parsed.GetHelpText(width < 20 ? 72 : width);
                    Console.Error.WriteLine(text);

                    return null;
                }

                //var u = result.GetArguments();

                //var p = result.GetActiveParameters();

                //var o = result.GetActiveOptions();

                //var a = result.GetActiveArguments();

                //var h = result.GetHelpText();

                //foreach (var x in p)
                //{
                //    Console.WriteLine("{0}", x.Name);
                //}

                switch (command)
                {
                    case Commands.Build:
                        {
                            var sourceFiles = GatherSourceFiles(files, outputFolder);
                            var variables = GatherPreprocessorVariables(defines);
                            return new BuildCommand(sourceFiles, variables, locFiles, outputFile);
                        }

                    case Commands.Compile:
                        {
                            var sourceFiles = GatherSourceFiles(files, outputFolder);
                            var variables = GatherPreprocessorVariables(defines);
                            return new CompileCommand(sourceFiles, variables);
                        }

                    case Commands.Version:
                        return new VersionCommand();
                }

                //var preprocessorVariables = this.GatherPreprocessorVariables();

                //foreach (var sourceFile in sourceFiles)
                //{
                //    var document = preprocessor.Process(sourceFile.SourcePath, preprocessorVariables);

                //    var intermediate = compiler.Compile(document);

                //    intermediate.Save(sourceFile.OutputPath);
                //}
            }
            //catch (ArgumentSyntaxException e)
            //{
            //    if (IsHelpRequested(parsed))
            //    {
            //        var width = Console.WindowWidth - 2;
            //        var text = parsed.GetHelpText(width < 20 ? 72 : width);
            //        Console.Error.WriteLine(text);
            //    }
            //    else
            //    {
            //        Console.Error.WriteLine(e.Message);
            //    }
            //}

            return null;
        }

        //private static bool IsHelpRequested(ArgumentSyntax syntax)
        //{
        //    return syntax?.RemainingArguments
        //                 .Any(a => String.Equals(a, @"-?", StringComparison.Ordinal) ||
        //                           String.Equals(a, @"-h", StringComparison.Ordinal) ||
        //                           String.Equals(a, @"--help", StringComparison.Ordinal)) ?? false;
        //}
#endif

#if false
        private int Execute(string[] args)
        {
            try
            {
                string[] unparsed = this.ParseCommandLineAndLoadExtensions(args);

                if (!Messaging.Instance.EncounteredError)
                {
                    if (this.commandLine.ShowLogo)
                    {
                        AppCommon.DisplayToolHeader();
                    }

                    if (this.commandLine.ShowHelp)
                    {
                        Console.WriteLine(CandleStrings.HelpMessage);
                        AppCommon.DisplayToolFooter();
                    }
                    else
                    {
                        foreach (string arg in unparsed)
                        {
                            Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(arg));
                        }

                        this.Run();
                    }
                }
            }
            catch (WixException we)
            {
                Messaging.Instance.OnMessage(we.Error);
            }
            catch (Exception e)
            {
                Messaging.Instance.OnMessage(WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return Messaging.Instance.LastErrorNumber;
        }

        private string[] ParseCommandLineAndLoadExtensions(string[] args)
        {
            this.commandLine = new CandleCommandLine();
            string[] unprocessed = commandLine.Parse(args);
            if (Messaging.Instance.EncounteredError)
            {
                return unprocessed;
            }

            // Load extensions.
            ExtensionManager extensionManager = new ExtensionManager();
            foreach (string extension in this.commandLine.Extensions)
            {
                extensionManager.Load(extension);
            }

            // Preprocessor extension command line processing.
            this.preprocessorExtensions = extensionManager.Create<IPreprocessorExtension>();
            foreach (IExtensionCommandLine pce in this.preprocessorExtensions.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
            {
                pce.MessageHandler = Messaging.Instance;
                unprocessed = pce.ParseCommandLine(unprocessed);
            }

            // Compiler extension command line processing.
            this.compilerExtensions = extensionManager.Create<ICompilerExtension>();
            foreach (IExtensionCommandLine cce in this.compilerExtensions.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
            {
                cce.MessageHandler = Messaging.Instance;
                unprocessed = cce.ParseCommandLine(unprocessed);
            }

            // Extension data command line processing.
            this.extensionData = extensionManager.Create<IExtensionData>();
            foreach (IExtensionCommandLine dce in this.extensionData.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
            {
                dce.MessageHandler = Messaging.Instance;
                unprocessed = dce.ParseCommandLine(unprocessed);
            }

            return commandLine.ParsePostExtensions(unprocessed);
        }

        private void Run()
        {
            // Create the preprocessor and compiler
            Preprocessor preprocessor = new Preprocessor();
            preprocessor.CurrentPlatform = this.commandLine.Platform;

            foreach (string includePath in this.commandLine.IncludeSearchPaths)
            {
                preprocessor.IncludeSearchPaths.Add(includePath);
            }

            foreach (IPreprocessorExtension pe in this.preprocessorExtensions)
            {
                preprocessor.AddExtension(pe);
            }

            Compiler compiler = new Compiler();
            compiler.ShowPedanticMessages = this.commandLine.ShowPedanticMessages;
            compiler.CurrentPlatform = this.commandLine.Platform;

            foreach (IExtensionData ed in this.extensionData)
            {
                compiler.AddExtensionData(ed);
            }

            foreach (ICompilerExtension ce in this.compilerExtensions)
            {
                compiler.AddExtension(ce);
            }

            // Preprocess then compile each source file.
            foreach (CompileFile file in this.commandLine.Files)
            {
                // print friendly message saying what file is being compiled
                Console.WriteLine(file.SourcePath);

                // preprocess the source
                XDocument sourceDocument;
                try
                {
                    if (!String.IsNullOrEmpty(this.commandLine.PreprocessFile))
                    {
                        preprocessor.PreprocessOut = this.commandLine.PreprocessFile.Equals("con:", StringComparison.OrdinalIgnoreCase) ? Console.Out : new StreamWriter(this.commandLine.PreprocessFile);
                    }

                    sourceDocument = preprocessor.Process(file.SourcePath, this.commandLine.PreprocessorVariables);
                }
                finally
                {
                    if (null != preprocessor.PreprocessOut && Console.Out != preprocessor.PreprocessOut)
                    {
                        preprocessor.PreprocessOut.Close();
                    }
                }

                // If we're not actually going to compile anything, move on to the next file.
                if (null == sourceDocument || !String.IsNullOrEmpty(this.commandLine.PreprocessFile))
                {
                    continue;
                }

                // and now we do what we came here to do...
                Intermediate intermediate = compiler.Compile(sourceDocument);

                // save the intermediate to disk if no errors were found for this source file
                if (null != intermediate)
                {
                    intermediate.Save(file.OutputPath);
                }
            }
        }

        public interface IOptions
        {
            IEnumerable<SourceFile> SourceFiles { get; }
        }

        public class CompilerOptions : IOptions
        {
            public CompilerOptions(IEnumerable<SourceFile> sources)
            {
                this.SourceFiles = sources;
            }

            public IEnumerable<SourceFile> SourceFiles { get; private set; }
        }
#endif
    }
}
