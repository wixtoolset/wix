// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The main entry point for candle.
    /// </summary>
    public sealed class Candle
    {
        private CandleCommandLine commandLine;

        private IEnumerable<IPreprocessorExtension> preprocessorExtensions;
        private IEnumerable<ICompilerExtension> compilerExtensions;
        private IEnumerable<IExtensionData> extensionData;

        /// <summary>
        /// The main entry point for candle.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("CNDL", "candle.exe").Display += AppCommon.ConsoleDisplayMessage;

            Candle candle = new Candle();
            return candle.Execute(args);
        }

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
    }
}
