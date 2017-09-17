// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// Parse command line for candle.
    /// </summary>
    public class CandleCommandLine
    {
        public CandleCommandLine()
        {
            this.Platform = Platform.X86;

            this.ShowLogo = true;
            this.Extensions = new List<string>();
            this.Files = new List<CompileFile>();
            this.IncludeSearchPaths = new List<string>();
            this.PreprocessorVariables = new Dictionary<string, string>();
        }

        public Platform Platform { get; private set; }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowPedanticMessages { get; private set; }

        public string OutputFolder { get; private set; }

        public string OutputFile { get; private set; }

        public List<string> Extensions { get; private set; }

        public List<CompileFile> Files { get; private set; }

        public List<string> IncludeSearchPaths { get; private set; }

        public string PreprocessFile { get; private set; }

        public Dictionary<string, string> PreprocessorVariables { get; private set; }

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
                    if ('d' == parameter[0])
                    {
                        if (1 >= parameter.Length || '=' == parameter[1])
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidVariableDefinition(arg));
                            break;
                        }

                        parameter = arg.Substring(2);

                        string[] value = parameter.Split("=".ToCharArray(), 2);

                        if (this.PreprocessorVariables.ContainsKey(value[0]))
                        {
                            Messaging.Instance.OnMessage(WixErrors.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], this.PreprocessorVariables[value[0]]));
                            break;
                        }

                        if (1 == value.Length)
                        {
                            this.PreprocessorVariables.Add(value[0], String.Empty);
                        }
                        else
                        {
                            this.PreprocessorVariables.Add(value[0], value[1]);
                        }
                    }
                    else if ('I' == parameter[0])
                    {
                        this.IncludeSearchPaths.Add(parameter.Substring(1));
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            break;
                        }
                        else
                        {
                            this.Extensions.Add(args[i]);
                        }
                    }
                    else if ("nologo" == parameter)
                    {
                        this.ShowLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        string path = CommandLine.GetFileOrDirectory(parameter, args, ++i);

                        if (!String.IsNullOrEmpty(path))
                        {
                            if (path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal))
                            {
                                this.OutputFolder = path;
                            }
                            else
                            {
                                this.OutputFile = path;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ("pedantic" == parameter)
                    {
                        this.ShowPedanticMessages = true;
                    }
                    else if ("arch" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidPlatformParameter(parameter, String.Empty));
                            break;
                        }

                        if (String.Equals(args[i], "intel", StringComparison.OrdinalIgnoreCase) || String.Equals(args[i], "x86", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Platform = Platform.X86;
                        }
                        else if (String.Equals(args[i], "x64", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Platform = Platform.X64;
                        }
                        else if (String.Equals(args[i], "intel64", StringComparison.OrdinalIgnoreCase) || String.Equals(args[i], "ia64", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Platform = Platform.IA64;
                        }
                        else if (String.Equals(args[i], "arm", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Platform = Platform.ARM;
                        }
                        else
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidPlatformParameter(parameter, args[i]));
                        }
                    }
                    else if ('p' == parameter[0])
                    {
                        string file = parameter.Substring(1);
                        this.PreprocessFile = String.IsNullOrEmpty(file) ? "con:" : file;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.SuppressAllWarnings = true;
                            }
                            else
                            {
                                int suppressWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= suppressWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                                }

                                Messaging.Instance.SuppressWarningMessage(suppressWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                    }
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.WarningsAsError = true;
                            }
                            else
                            {
                                int elevateWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= elevateWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                                }

                                Messaging.Instance.ElevateWarningMessage(elevateWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                    }
                    else if ("v" == parameter)
                    {
                        Messaging.Instance.ShowVerboseMessages = true;
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
            List<string> files = new List<string>();

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
                    files.AddRange(CommandLine.GetFiles(arg, "Source"));
                }
            }

            if (0 == files.Count)
            {
                this.ShowHelp = true;
            }
            else
            {
                Dictionary<string, List<string>> sourcesForOutput = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    string sourceFileName = Path.GetFileName(file);

                    CompileFile compileFile = new CompileFile();
                    compileFile.SourcePath = Path.GetFullPath(file);

                    if (null != this.OutputFile)
                    {
                        compileFile.OutputPath = this.OutputFile;
                    }
                    else if (null != this.OutputFolder)
                    {
                        compileFile.OutputPath = Path.Combine(this.OutputFolder, Path.ChangeExtension(sourceFileName, ".wixobj"));
                    }
                    else
                    {
                        compileFile.OutputPath = Path.ChangeExtension(sourceFileName, ".wixobj");
                    }

                    // Track which source files result in a given output file, to ensure we aren't
                    // overwriting the output.
                    List<string> sources;
                    string targetPath = Path.GetFullPath(compileFile.OutputPath);
                    if (!sourcesForOutput.TryGetValue(targetPath, out sources))
                    {
                        sources = new List<string>();
                        sourcesForOutput.Add(targetPath, sources);
                    }

                    sources.Add(compileFile.SourcePath);

                    this.Files.Add(compileFile);
                }

                // Show an error for every output file that had more than 1 source file.
                foreach (KeyValuePair<string, List<string>> outputSources in sourcesForOutput)
                {
                    if (1 < outputSources.Value.Count)
                    {
                        string sourceFiles = String.Join(", ", outputSources.Value);
                        Messaging.Instance.OnMessage(WixErrors.DuplicateSourcesForOutput(sourceFiles, outputSources.Key));
                    }
                }
            }

            return unprocessed.ToArray();
        }
    }
}
