// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The main entry point for light.
    /// </summary>
    public sealed class Light
    {
        LightCommandLine commandLine;
        //private IEnumerable<IBinderExtension> binderExtensions;
        //private IEnumerable<IBinderFileManager> fileManagers;

        /// <summary>
        /// The main entry point for light.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            var serviceProvider = new WixToolsetServiceProvider();

            var listener = new ConsoleMessageListener("WIX", "light.exe");

            Light light = new Light();
            return light.Run(serviceProvider, listener, args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        public int Run(IServiceProvider serviceProvider, IMessageListener listener, string[] args)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            try
            {
                var unparsed = this.ParseCommandLineAndLoadExtensions(serviceProvider, messaging, args);

                if (!messaging.EncounteredError)
                {
                    if (this.commandLine.ShowLogo)
                    {
                        AppCommon.DisplayToolHeader();
                    }

                    if (this.commandLine.ShowHelp)
                    {
                        PrintHelp();
                        AppCommon.DisplayToolFooter();
                    }
                    else
                    {
                        foreach (string arg in unparsed)
                        {
                            messaging.Write(WarningMessages.UnsupportedCommandLineArgument(arg));
                        }

                        this.Bind(serviceProvider, messaging);
                    }
                }
            }
            catch (WixException we)
            {
                messaging.Write(we.Error);
            }
            catch (Exception e)
            {
                messaging.Write(ErrorMessages.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return messaging.LastErrorNumber;
        }

        /// <summary>
        /// Parse command line and load all the extensions.
        /// </summary>
        /// <param name="args">Command line arguments to be parsed.</param>
        private IEnumerable<string> ParseCommandLineAndLoadExtensions(IServiceProvider serviceProvider, IMessaging messaging, string[] args)
        {
            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            var extensionManager = CreateExtensionManagerWithStandardBackends(serviceProvider, arguments.Extensions);

            var context = serviceProvider.GetService<ICommandLineContext>();
            context.ExtensionManager = extensionManager;
            context.Messaging = messaging;
            context.Arguments = arguments;

            this.commandLine = new LightCommandLine(messaging);
            var unprocessed = this.commandLine.Parse(context);

            return unprocessed;
        }

        private void Bind(IServiceProvider serviceProvider, IMessaging messaging)
        {
            var output = this.LoadWixout(messaging);

            if (messaging.EncounteredError)
            {
                return;
            }

            var intermediateFolder = this.commandLine.IntermediateFolder;
            if (String.IsNullOrEmpty(intermediateFolder))
            {
                intermediateFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }

            var localizations = this.LoadLocalizationFiles(messaging, this.commandLine.LocalizationFiles);

            if (messaging.EncounteredError)
            {
                return;
            }

            ResolveResult resolveResult;
            {
                var resolver = new Resolver(serviceProvider);
                resolver.BindPaths = this.commandLine.BindPaths;
                resolver.IntermediateFolder = intermediateFolder;
                resolver.IntermediateRepresentation = output;
                resolver.Localizations = localizations;

                resolveResult = resolver.Execute();
            }

            if (messaging.EncounteredError)
            {
                return;
            }

            BindResult bindResult;
            {
                var binder = new Binder(serviceProvider);
                binder.CabbingThreadCount = this.commandLine.CabbingThreadCount;
                binder.CabCachePath = this.commandLine.CabCachePath;
                binder.Codepage = resolveResult.Codepage;
                binder.DefaultCompressionLevel = this.commandLine.DefaultCompressionLevel;
                binder.DelayedFields = resolveResult.DelayedFields;
                binder.ExpectedEmbeddedFiles = resolveResult.ExpectedEmbeddedFiles;
                binder.Ices = this.commandLine.Ices;
                binder.IntermediateFolder = intermediateFolder;
                binder.IntermediateRepresentation = resolveResult.IntermediateRepresentation;
                binder.OutputPath = this.commandLine.OutputFile;
                binder.OutputPdbPath = Path.ChangeExtension(this.commandLine.OutputFile, ".wixpdb");
                binder.SuppressIces = this.commandLine.SuppressIces;
                binder.SuppressValidation = this.commandLine.SuppressValidation;

                bindResult = binder.Execute();
            }

            if (messaging.EncounteredError)
            {
                return;
            }

            {
                var layout = new Layout(serviceProvider);
                layout.FileTransfers = bindResult.FileTransfers;
                layout.ContentFilePaths = bindResult.ContentFilePaths;
                layout.ContentsFile = this.commandLine.ContentsFile;
                layout.OutputsFile = this.commandLine.OutputsFile;
                layout.BuiltOutputsFile = this.commandLine.BuiltOutputsFile;
                layout.SuppressAclReset = this.commandLine.SuppressAclReset;

                layout.Execute();
            }
        }

        private void Run(IMessaging messaging)
        {
#if false
            // Initialize the variable resolver from the command line.
            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            foreach (var wixVar in this.commandLine.Variables)
            {
                wixVariableResolver.AddVariable(wixVar.Key, wixVar.Value);
            }

            // Initialize the linker from the command line.
            Linker linker = new Linker();
            linker.UnreferencedSymbolsFile = this.commandLine.UnreferencedSymbolsFile;
            linker.ShowPedanticMessages = this.commandLine.ShowPedanticMessages;
            linker.WixVariableResolver = wixVariableResolver;

            foreach (IExtensionData data in this.extensionData)
            {
                linker.AddExtensionData(data);
            }

            // Initialize the binder from the command line.
            WixToolset.Binder binder = new WixToolset.Binder();
            binder.CabCachePath = this.commandLine.CabCachePath;
            binder.ContentsFile = this.commandLine.ContentsFile;
            binder.BuiltOutputsFile = this.commandLine.BuiltOutputsFile;
            binder.OutputsFile = this.commandLine.OutputsFile;
            binder.WixprojectFile = this.commandLine.WixprojectFile;
            binder.BindPaths.AddRange(this.commandLine.BindPaths);
            binder.CabbingThreadCount = this.commandLine.CabbingThreadCount;
            if (this.commandLine.DefaultCompressionLevel.HasValue)
            {
                binder.DefaultCompressionLevel = this.commandLine.DefaultCompressionLevel.Value;
            }
            binder.Ices.AddRange(this.commandLine.Ices);
            binder.SuppressIces.AddRange(this.commandLine.SuppressIces);
            binder.SuppressAclReset = this.commandLine.SuppressAclReset;
            binder.SuppressLayout = this.commandLine.SuppressLayout;
            binder.SuppressValidation = this.commandLine.SuppressValidation;
            binder.PdbFile = this.commandLine.SuppressWixPdb ? null : this.commandLine.PdbFile;
            binder.TempFilesLocation = AppCommon.GetTempLocation();
            binder.WixVariableResolver = wixVariableResolver;

            foreach (IBinderExtension extension in this.binderExtensions)
            {
                binder.AddExtension(extension);
            }

            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                binder.AddExtension(fileManager);
            }

            // Initialize the localizer.
            Localizer localizer = this.InitializeLocalization(linker.TableDefinitions);
            if (messaging.EncounteredError)
            {
                return;
            }

            wixVariableResolver.Localizer = localizer;
            linker.Localizer = localizer;
            binder.Localizer = localizer;

            // Loop through all the believed object files.
            List<Section> sections = new List<Section>();
            Output output = null;
            foreach (string inputFile in this.commandLine.Files)
            {
                string inputFileFullPath = Path.GetFullPath(inputFile);
                FileFormat format = FileStructure.GuessFileFormatFromExtension(Path.GetExtension(inputFileFullPath));
                bool retry;
                do
                {
                    retry = false;

                    try
                    {
                        switch (format)
                        {
                            case FileFormat.Wixobj:
                                Intermediate intermediate = Intermediate.Load(inputFileFullPath, linker.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                sections.AddRange(intermediate.Sections);
                                break;

                            case FileFormat.Wixlib:
                                Library library = Library.Load(inputFileFullPath, linker.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                AddLibraryLocalizationsToLocalizer(library, this.commandLine.Cultures, localizer);
                                sections.AddRange(library.Sections);
                                break;

                            default:
                                output = Output.Load(inputFileFullPath, this.commandLine.SuppressVersionCheck);
                                break;
                        }
                    }
                    catch (WixUnexpectedFileFormatException e)
                    {
                        format = e.FileFormat;
                        retry = (FileFormat.Wixobj == format || FileFormat.Wixlib == format || FileFormat.Wixout == format); // .wixobj, .wixout and .wixout are supported by light.
                        if (!retry)
                        {
                            messaging.OnMessage(e.Error);
                        }
                    }
                } while (retry);
            }

            // Stop processing if any errors were found loading object files.
            if (messaging.EncounteredError)
            {
                return;
            }

            // and now for the fun part
            if (null == output)
            {
                OutputType expectedOutputType = OutputType.Unknown;
                if (!String.IsNullOrEmpty(this.commandLine.OutputFile))
                {
                    expectedOutputType = Output.GetOutputType(Path.GetExtension(this.commandLine.OutputFile));
                }

                output = linker.Link(sections, expectedOutputType);

                // If an error occurred during linking, stop processing.
                if (null == output)
                {
                    return;
                }
            }
            else if (0 != sections.Count)
            {
                throw new InvalidOperationException(LightStrings.EXP_CannotLinkObjFilesWithOutpuFile);
            }

            bool tidy = true; // clean up after ourselves by default.
            try
            {
                // only output the xml if its a patch build or user specfied to only output wixout
                string outputFile = this.commandLine.OutputFile;
                string outputExtension = Path.GetExtension(outputFile);
                if (this.commandLine.OutputXml || OutputType.Patch == output.Type)
                {
                    if (String.IsNullOrEmpty(outputExtension) || outputExtension.Equals(".wix", StringComparison.Ordinal))
                    {
                        outputExtension = (OutputType.Patch == output.Type) ? ".wixmsp" : ".wixout";
                        outputFile = Path.ChangeExtension(outputFile, outputExtension);
                    }

                    output.Save(outputFile);
                }
                else // finish creating the MSI/MSM
                {
                    if (String.IsNullOrEmpty(outputExtension) || outputExtension.Equals(".wix", StringComparison.Ordinal))
                    {
                        outputExtension = Output.GetExtension(output.Type);
                        outputFile = Path.ChangeExtension(outputFile, outputExtension);
                    }

                    binder.Bind(output, outputFile);
                }
            }
            catch (WixException we) // keep files around for debugging IDT issues.
            {
                if (we is WixInvalidIdtException)
                {
                    tidy = false;
                }

                throw;
            }
            catch (Exception) // keep files around for debugging unexpected exceptions.
            {
                tidy = false;
                throw;
            }
            finally
            {
                if (null != binder)
                {
                    binder.Cleanup(tidy);
                }
            }

            return;
#endif
        }

#if false
        private Localizer InitializeLocalization(TableDefinitionCollection tableDefinitions)
        {
            Localizer localizer = null;

            // Instantiate the localizer and load any localization files.
            if (!this.commandLine.SuppressLocalization || 0 < this.commandLine.LocalizationFiles.Count || null != this.commandLine.Cultures || !this.commandLine.OutputXml)
            {
                List<Localization> localizations = new List<Localization>();

                // Load each localization file.
                foreach (string localizationFile in this.commandLine.LocalizationFiles)
                {
                    Localization localization = Localizer.ParseLocalizationFile(localizationFile, tableDefinitions);
                    if (null != localization)
                    {
                        localizations.Add(localization);
                    }
                }

                localizer = new Localizer();
                if (null != this.commandLine.Cultures)
                {
                    // Alocalizations in order specified in cultures.
                    foreach (string culture in this.commandLine.Cultures)
                    {
                        foreach (Localization localization in localizations)
                        {
                            if (culture.Equals(localization.Culture, StringComparison.OrdinalIgnoreCase))
                            {
                                localizer.AddLocalization(localization);
                            }
                        }
                    }
                }
                else // no cultures specified, so try neutral culture and if none of those add all loc files.
                {
                    bool neutralFound = false;
                    foreach (Localization localization in localizations)
                    {
                        if (String.IsNullOrEmpty(localization.Culture))
                        {
                            // If a neutral wxl was provided use it.
                            localizer.AddLocalization(localization);
                            neutralFound = true;
                        }
                    }

                    if (!neutralFound)
                    {
                        // No cultures were specified and no neutral wxl are available, include all of the loc files.
                        foreach (Localization localization in localizations)
                        {
                            localizer.AddLocalization(localization);
                        }
                    }
                }

                // Load localizations provided by extensions with data.
                foreach (IExtensionData data in this.extensionData)
                {
                    Library library = data.GetLibrary(tableDefinitions);
                    if (null != library)
                    {
                        // Load the extension's default culture if it provides one and no cultures were specified.
                        string[] extensionCultures = this.commandLine.Cultures;
                        if (null == extensionCultures && null != data.DefaultCulture)
                        {
                            extensionCultures = new string[] { data.DefaultCulture };
                        }

                        AddLibraryLocalizationsToLocalizer(library, extensionCultures, localizer);
                    }
                }
            }

            return localizer;
        }

        private void AddLibraryLocalizationsToLocalizer(Library library, string[] cultures, Localizer localizer)
        {
            foreach (Localization localization in library.GetLocalizations(cultures))
            {
                localizer.AddLocalization(localization);
            }
        }
#endif

        private bool TryParseCommandLineArgumentWithExtension(string arg, IEnumerable<IExtensionCommandLine> extensions)
        {
            foreach (var extension in extensions)
            {
                // TODO: decide what to do with "IParseCommandLine" argument.
                if (extension.TryParseArgument(null, arg))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<Localization> LoadLocalizationFiles(IMessaging messaging, IEnumerable<string> locFiles)
        {
            foreach (var loc in locFiles)
            {
                var localization = Localizer.ParseLocalizationFile(messaging, loc);

                yield return localization;
            }
        }

        private Intermediate LoadWixout(IMessaging messaging)
        {
            var path = this.commandLine.Files.Single();

            return Intermediate.Load(path);
        }

        private static IExtensionManager CreateExtensionManagerWithStandardBackends(IServiceProvider serviceProvider, IEnumerable<string> extensions)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            foreach (var extension in extensions)
            {
                extensionManager.Load(extension);
            }

            return extensionManager;
        }

        private static void PrintHelp()
        {
            string lightArgs = LightStrings.CommandLineArguments;

            Console.WriteLine(String.Format(LightStrings.HelpMessage, lightArgs));
        }

        private class ConsoleMessageListener : IMessageListener
        {
            public ConsoleMessageListener(string shortName, string longName)
            {
                this.ShortAppName = shortName;
                this.LongAppName = longName;

                PrepareConsoleForLocalization();
            }

            public string LongAppName { get; }

            public string ShortAppName { get; }

            public void Write(Message message)
            {
                var filename = message.SourceLineNumbers?.FileName ?? this.LongAppName;
                var line = message.SourceLineNumbers?.LineNumber ?? -1;
                var type = message.Level.ToString().ToLowerInvariant();
                var output = message.Level >= MessageLevel.Warning ? Console.Out : Console.Error;

                if (line > 0)
                {
                    filename = String.Concat(filename, "(", line, ")");
                }

                output.WriteLine("{0} : {1} {2}{3:0000}: {4}", filename, type, this.ShortAppName, message.Id, message.ToString());
            }

            public void Write(string message)
            {
                Console.Out.WriteLine(message);
            }

            private static void PrepareConsoleForLocalization()
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture.GetConsoleFallbackUICulture();

                if (Console.OutputEncoding.CodePage != Encoding.UTF8.CodePage &&
                    Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.OEMCodePage &&
                    Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.ANSICodePage)
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                }
            }
        }
    }
}
