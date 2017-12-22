// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Core.CommandLine;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public class LightCommandLine
    {
        public LightCommandLine(IMessaging messaging)
        {
            this.Messaging = messaging;
            this.ShowLogo = true;
            this.Tidy = true;

            this.CubeFiles = new List<string>();
            this.SuppressIces = new List<string>();
            this.Ices = new List<string>();
            this.BindPaths = new List<BindPath>();
            this.Extensions = new List<string>();
            this.Files = new List<string>();
            this.LocalizationFiles = new List<string>();
            this.Variables = new Dictionary<string, string>();
        }

        public IMessaging Messaging { get; }

        public string PdbFile { get; private set; }

        public CompressionLevel? DefaultCompressionLevel { get; set; }

        public bool SuppressAclReset { get; private set; }

        public bool SuppressLayout { get; private set; }

        public bool SuppressWixPdb { get; private set; }

        public bool SuppressValidation { get; private set; }

        public string IntermediateFolder { get; private set; }

        public string OutputsFile { get; private set; }

        public string BuiltOutputsFile { get; private set; }

        public string WixprojectFile { get; private set; }

        public string ContentsFile { get; private set; }

        public List<string> Ices { get; private set; }

        public string CabCachePath { get; private set; }

        public int CabbingThreadCount { get; private set; }

        public List<string> CubeFiles { get; private set; }

        public List<string> SuppressIces { get; private set; }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowPedanticMessages { get; private set; }

        public bool SuppressLocalization { get; private set; }

        public bool SuppressVersionCheck { get; private set; }

        public string[] Cultures { get; private set; }

        public string OutputFile { get; private set; }

        public bool OutputXml { get; private set; }

        public List<BindPath> BindPaths { get; private set; }

        public List<string> Extensions { get; private set; }

        public List<string> Files { get; private set; }

        public List<string> LocalizationFiles { get; private set; }

        public bool Tidy { get; private set; }

        public string UnreferencedSymbolsFile { get; private set; }

        public IDictionary<string, string> Variables { get; private set; }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        public string[] Parse(string[] args)
        {
            List<string> unprocessed = new List<string>();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if (1 == arg.Length) // treat '-' and '@' as filenames when by themselves.
                {
                    unprocessed.Add(arg);
                }
                else if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if (parameter.Equals("b", StringComparison.Ordinal))
                    {
                        if (!CommandLineHelper.IsValidArg(args, ++i))
                        {
                            break;
                        }

                        var bindPath = BindPath.Parse(args[i]);

                        this.BindPaths.Add(bindPath);
                    }
                    else if (parameter.StartsWith("cultures:", StringComparison.Ordinal))
                    {
                        string culturesString = arg.Substring(10).ToLower(CultureInfo.InvariantCulture);

                        // When null is used treat it as if cultures wasn't specified.
                        // This is needed for batching over the light task when using MSBuild which doesn't
                        // support empty items
                        if (culturesString.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Cultures = null;
                        }
                        else
                        {
                            this.Cultures = culturesString.Split(';', ',');

                            for (int c = 0; c < this.Cultures.Length; ++c)
                            {
                                // Neutral is different from null. For neutral we still want to do WXL filtering.
                                // Set the culture to the empty string = identifier for the invariant culture
                                if (this.Cultures[c].Equals("neutral", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.Cultures[c] = String.Empty;
                                }
                            }
                        }
                    }
                    else if (parameter.StartsWith("dcl:", StringComparison.Ordinal))
                    {
                        string defaultCompressionLevel = arg.Substring(5);

                        if (String.IsNullOrEmpty(defaultCompressionLevel))
                        {
                            break;
                        }
                        else if (Enum.TryParse(defaultCompressionLevel, true, out CompressionLevel compressionLevel))
                        {
                            this.DefaultCompressionLevel = compressionLevel;
                        }
                    }
                    else if (parameter.StartsWith("d", StringComparison.Ordinal))
                    {
                        parameter = arg.Substring(2);
                        string[] value = parameter.Split("=".ToCharArray(), 2);

                        string preexisting;
                        if (1 == value.Length)
                        {
                            this.Messaging.Write(ErrorMessages.ExpectedWixVariableValue(value[0]));
                        }
                        else if (this.Variables.TryGetValue(value[0], out preexisting))
                        {
                            this.Messaging.Write(ErrorMessages.WixVariableCollision(null, value[0]));
                        }
                        else
                        {
                            this.Variables.Add(value[0], value[1]);
                        }
                    }
                    else if (parameter.Equals("ext", StringComparison.Ordinal))
                    {
                        if (!CommandLineHelper.IsValidArg(args, ++i))
                        {
                            this.Messaging.Write(ErrorMessages.TypeSpecificationForExtensionRequired("-ext"));
                            break;
                        }

                        this.Extensions.Add(args[i]);
                    }
                    else if (parameter.Equals("loc", StringComparison.Ordinal))
                    {
                        string locFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);
                        if (String.IsNullOrEmpty(locFile))
                        {
                            break;
                        }

                        this.LocalizationFiles.Add(locFile);
                    }
                    else if (parameter.Equals("nologo", StringComparison.Ordinal))
                    {
                        this.ShowLogo = false;
                    }
                    else if (parameter.Equals("notidy", StringComparison.Ordinal))
                    {
                        this.Tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.OutputFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);
                        if (String.IsNullOrEmpty(this.OutputFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("pedantic", StringComparison.Ordinal))
                    {
                        this.ShowPedanticMessages = true;
                    }
                    else if (parameter.Equals("sloc", StringComparison.Ordinal))
                    {
                        this.SuppressLocalization = true;
                    }
                    else if (parameter.Equals("usf", StringComparison.Ordinal))
                    {
                        this.UnreferencedSymbolsFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.UnreferencedSymbolsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("xo", StringComparison.Ordinal))
                    {
                        this.OutputXml = true;
                    }
                    else if (parameter.Equals("cc", StringComparison.Ordinal))
                    {
                        this.CabCachePath = CommandLineHelper.GetDirectory(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.CabCachePath))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("ct", StringComparison.Ordinal))
                    {
                        if (!CommandLineHelper.IsValidArg(args, ++i))
                        {
                            this.Messaging.Write(ErrorMessages.IllegalCabbingThreadCount(String.Empty));
                            break;
                        }

                        int ct = 0;
                        if (!Int32.TryParse(args[i], out ct) || 0 >= ct)
                        {
                            this.Messaging.Write(ErrorMessages.IllegalCabbingThreadCount(args[i]));
                            break;
                        }

                        this.CabbingThreadCount = ct;
                        this.Messaging.Write(VerboseMessages.SetCabbingThreadCount(this.CabbingThreadCount.ToString()));
                    }
                    else if (parameter.Equals("cub", StringComparison.Ordinal))
                    {
                        string cubeFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(cubeFile))
                        {
                            break;
                        }

                        this.CubeFiles.Add(cubeFile);
                    }
                    else if (parameter.StartsWith("ice:", StringComparison.Ordinal))
                    {
                        this.Ices.Add(parameter.Substring(4));
                    }
                    else if (parameter.Equals("intermediatefolder", StringComparison.OrdinalIgnoreCase))
                    {
                        this.IntermediateFolder = CommandLineHelper.GetDirectory(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.IntermediateFolder))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("contentsfile", StringComparison.Ordinal))
                    {
                        this.ContentsFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.ContentsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("outputsfile", StringComparison.Ordinal))
                    {
                        this.OutputsFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.OutputsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("builtoutputsfile", StringComparison.Ordinal))
                    {
                        this.BuiltOutputsFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.BuiltOutputsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("wixprojectfile", StringComparison.Ordinal))
                    {
                        this.WixprojectFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.WixprojectFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("pdbout", StringComparison.Ordinal))
                    {
                        this.PdbFile = CommandLineHelper.GetFile(parameter, this.Messaging, args, ++i);

                        if (String.IsNullOrEmpty(this.PdbFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.StartsWith("sice:", StringComparison.Ordinal))
                    {
                        this.SuppressIces.Add(parameter.Substring(5));
                    }
                    else if (parameter.Equals("sl", StringComparison.Ordinal))
                    {
                        this.SuppressLayout = true;
                    }
                    else if (parameter.Equals("spdb", StringComparison.Ordinal))
                    {
                        this.SuppressWixPdb = true;
                    }
                    else if (parameter.Equals("sacl", StringComparison.Ordinal))
                    {
                        this.SuppressAclReset = true;
                    }
                    else if (parameter.Equals("sval", StringComparison.Ordinal))
                    {
                        this.SuppressValidation = true;
                    }
                    else if ("sv" == parameter)
                    {
                        this.SuppressVersionCheck = true;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        if (0 == paramArg.Length)
                        {
                            this.Messaging.SuppressAllWarnings = true;
                        }
                        else
                        {
                            int suppressWarning = 0;
                            if (!Int32.TryParse(paramArg, out suppressWarning) || 0 >= suppressWarning)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalSuppressWarningId(paramArg));
                            }
                            else
                            {
                                this.Messaging.SuppressWarningMessage(suppressWarning);
                            }
                        }
                    }
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        if (0 == paramArg.Length)
                        {
                            this.Messaging.WarningsAsError = true;
                        }
                        else
                        {
                            int elevateWarning = 0;
                            if (!Int32.TryParse(paramArg, out elevateWarning) || 0 >= elevateWarning)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalWarningIdAsError(paramArg));
                            }
                            else
                            {
                                this.Messaging.ElevateWarningMessage(elevateWarning);
                            }
                        }
                    }
                    else if ("v" == parameter)
                    {
                        this.Messaging.ShowVerboseMessages = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.ShowHelp = true;
                        break;
                    }
                    else
                    {
                        unprocessed.Add(arg);
                    }
                }
                else if ('@' == arg[0])
                {
                    string[] parsedArgs = CommandLineResponseFile.Parse(arg.Substring(1));
                    string[] unparsedArgs = this.Parse(parsedArgs);
                    unprocessed.AddRange(unparsedArgs);
                }
                else
                {
                    unprocessed.Add(arg);
                }
            }

            return unprocessed.ToArray();
        }

        public string[] ParsePostExtensions(string[] remaining)
        {
            List<string> unprocessed = new List<string>();

            for (int i = 0; i < remaining.Length; ++i)
            {
                string arg = remaining[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if (1 < arg.Length && ('-' == arg[0] || '/' == arg[0]))
                {
                    unprocessed.Add(arg);
                }
                else
                {
                    this.Files.AddRange(CommandLineHelper.GetFiles(arg, "Source"));
                }
            }

            if (0 == this.Files.Count)
            {
                this.ShowHelp = true;
            }
            else if (String.IsNullOrEmpty(this.OutputFile))
            {
                if (1 < this.Files.Count)
                {
                    this.Messaging.Write(ErrorMessages.MustSpecifyOutputWithMoreThanOneInput());
                }

                // After the linker tells us what the output type actually is, we'll change the ".wix" to the correct extension.
                this.OutputFile = Path.ChangeExtension(Path.GetFileName(this.Files[0]), ".wix");

                // Add the directories of the input files as unnamed bind paths.
                foreach (string file in this.Files)
                {
                    BindPath bindPath = new BindPath(Path.GetDirectoryName(Path.GetFullPath(file)));
                    this.BindPaths.Add(bindPath);
                }
            }

            if (!this.SuppressWixPdb && String.IsNullOrEmpty(this.PdbFile) && !String.IsNullOrEmpty(this.OutputFile))
            {
                this.PdbFile = Path.ChangeExtension(this.OutputFile, ".wixpdb");
            }

            return unprocessed.ToArray();
        }
    }
}
