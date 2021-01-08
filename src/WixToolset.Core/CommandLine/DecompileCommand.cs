// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class DecompileCommand : ICommandLineCommand
    {
        private readonly CommandLine commandLine;

        public DecompileCommand(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.commandLine = new CommandLine(this.Messaging);
        }

        public bool ShowLogo => this.commandLine.ShowLogo;

        public bool StopParsing => this.commandLine.ShowHelp;

        private IWixToolsetServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        public Task<int> ExecuteAsync(CancellationToken _)
        {
            if (this.commandLine.ShowHelp || String.IsNullOrEmpty(this.commandLine.DecompileFilePath))
            {
                Console.WriteLine("TODO: Show decompile command help");
                return Task.FromResult(-1);
            }

            var context = this.ServiceProvider.GetService<IDecompileContext>();
            context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().GetServices<IDecompilerExtension>();
            context.DecompilePath = this.commandLine.DecompileFilePath;
            context.DecompileType = this.commandLine.CalculateDecompileType();
            context.IntermediateFolder = this.commandLine.CalculateIntermedateFolder();
            context.OutputPath = this.commandLine.CalculateOutputPath();

            try
            {
                var decompiler = this.ServiceProvider.GetService<IDecompiler>();
                var result = decompiler.Decompile(context);

                if (!this.Messaging.EncounteredError)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(context.OutputPath)));
                    result.Document.Save(context.OutputPath, SaveOptions.OmitDuplicateNamespaces);
                }
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
            }

            if (this.Messaging.EncounteredError)
            {
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            return this.commandLine.TryParseArgument(argument, parser);
        }

        private class CommandLine
        {
            public CommandLine(IMessaging messaging)
            {
                this.Messaging = messaging;
            }

            private IMessaging Messaging { get; }

            public string DecompileFilePath { get; private set; }

            public string DecompileType { get; private set; }

            public Platform Platform { get; private set; }

            public bool ShowLogo { get; private set; }

            public bool ShowHelp { get; private set; }

            public string IntermediateFolder { get; private set; }

            public string OutputFile { get; private set; }

            public bool TryParseArgument(string arg, ICommandLineParser parser)
            {
                if (parser.IsSwitch(arg))
                {
                    var parameter = arg.Substring(1);
                    switch (parameter.ToLowerInvariant())
                    {
                    case "?":
                    case "h":
                    case "help":
                        this.ShowHelp = true;
                        return true;

                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(arg);
                        return true;

                    case "o":
                    case "out":
                        this.OutputFile = parser.GetNextArgumentAsFilePathOrError(arg);
                        return true;

                    case "nologo":
                        this.ShowLogo = false;
                        return true;

                    case "v":
                    case "verbose":
                        this.Messaging.ShowVerboseMessages = true;
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

                    if (parameter.StartsWith("wx"))
                    {
                        this.ParseWarningAsError(parameter, "wx".Length, parser);
                        return true;
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(this.DecompileFilePath))
                    {
                        this.DecompileFilePath = parser.GetArgumentAsFilePathOrError(arg, "decompile file");
                        return true;
                    }
                    else if (String.IsNullOrEmpty(this.OutputFile))
                    {
                        this.OutputFile = parser.GetArgumentAsFilePathOrError(arg, "output file");
                        return true;
                    }
                }

                return false;
            }

            public OutputType CalculateDecompileType()
            {
                if (String.IsNullOrEmpty(this.DecompileType))
                {
                    this.DecompileType = Path.GetExtension(this.DecompileFilePath);
                }

                switch (this.DecompileType.ToLowerInvariant())
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

            public string CalculateIntermedateFolder()
            {
                return String.IsNullOrEmpty(this.IntermediateFolder) ? Path.GetTempPath() : this.IntermediateFolder;
            }

            public string CalculateOutputPath()
            {
                return String.IsNullOrEmpty(this.OutputFile) ? Path.ChangeExtension(this.DecompileFilePath, ".wxs") : this.OutputFile;
            }

            private void ParseWarningAsError(string parameter, int offset, ICommandLineParser parser)
            {
                var paramArg = parameter.Substring(offset);
                if (paramArg.Length == 0)
                {
                    this.Messaging.WarningsAsError = true;
                }
                else if (Int32.TryParse(paramArg, out var elevateWarning) && elevateWarning > 0)
                {
                    this.Messaging.SuppressWarningMessage(elevateWarning);
                }
                else
                {
                    parser.ReportErrorArgument(parameter, ErrorMessages.IllegalSuppressWarningId(paramArg));
                }
            }
        }
    }
}
