// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
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

    internal class DecompilerSubcommand : WindowsInstallerSubcommandBase
    {
        public DecompilerSubcommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private string InputPath { get; set; }

        private string DecompileType { get; set; }

        private string IntermediateFolder { get; set; }

        private string OutputPath { get; set; }

        private string ExportBasePath { get; set; }

        private bool SaveAsData { get; set; }

        private bool SuppressCustomTables { get; set; }

        private bool SuppressDroppingEmptyTables { get; set; }

        private bool SuppressRelativeActionSequencing { get; set; }

        private bool SuppressUI { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Converts a Windows Installer database back into source code.", "msi decompile [options] inputfile", new[]
            {
                new CommandLineHelpSwitch("-cub", "Optional path to a custom validation .CUBe file."),
                new CommandLineHelpSwitch("-data", "Save output as data instead of as a source file."),
                new CommandLineHelpSwitch("-sct", "Suppress decompiling custom tables."),
                new CommandLineHelpSwitch("-sdet", "Suppress dropping empty tables."),
                new CommandLineHelpSwitch("-sras", "Suppress relative action sequencing."),
                new CommandLineHelpSwitch("-sui", "Suppress decompiling UI tables."),
                new CommandLineHelpSwitch("-type", "Optional specify the input file type: msi or msm. If not specified, type will be inferred by file extension."),
                new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified %TMP% will be used."),
                new CommandLineHelpSwitch("-out", "-o", "Path to output the decompiled output file. If not specified, outputs next to inputfile"),
                new CommandLineHelpSwitch("-x", "Folder to export embedded binaries and icons to."),
            });
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                this.Messaging.Write(ErrorMessages.FilePathRequired("input MSI or MSM database"));
            }
            else if (!this.TryCalculateDecompileType(out var decompileType))
            {
                this.Messaging.Write(WindowsInstallerBackendErrors.UnknownDecompileType(this.DecompileType, this.InputPath));
            }
            else
            {
                if (String.IsNullOrEmpty(this.IntermediateFolder))
                {
                    this.IntermediateFolder = Path.GetTempPath();
                }

                if (String.IsNullOrEmpty(this.OutputPath))
                {
                    var defaultExtension = this.CalculateExtensionFromDecompileType(decompileType);

                    this.OutputPath = Path.ChangeExtension(this.InputPath, defaultExtension);
                }

                var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();
                var creator = this.ServiceProvider.GetService<ISymbolDefinitionCreator>();

                var context = this.ServiceProvider.GetService<IWindowsInstallerDecompileContext>();
                context.Extensions = extensionManager.GetServices<IWindowsInstallerDecompilerExtension>();
                context.ExtensionData = extensionManager.GetServices<IExtensionData>();
                context.DecompilePath = this.InputPath;
                context.DecompileType = decompileType;
                context.IntermediateFolder = this.IntermediateFolder;
                context.SymbolDefinitionCreator = creator;
                context.OutputPath = this.OutputPath;

                context.ExtractFolder = this.ExportBasePath;
                context.SuppressCustomTables = this.SuppressCustomTables;
                context.SuppressDroppingEmptyTables = this.SuppressDroppingEmptyTables;
                context.SuppressRelativeActionSequencing = this.SuppressRelativeActionSequencing;
                context.SuppressUI = this.SuppressUI;

                try
                {
                    var decompiler = this.ServiceProvider.GetService<IWindowsInstallerDecompiler>();
                    var result = decompiler.Decompile(context);

                    if (!this.Messaging.EncounteredError)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(context.OutputPath)));
                        if (this.SaveAsData || result.Document == null)
                        {
                            using (var output = WixOutput.Create(context.OutputPath))
                            {
                                result.Data.Save(output);
                            }
                        }
                        else
                        {
                            result.Document.Save(context.OutputPath, SaveOptions.OmitDuplicateNamespaces);
                        }
                    }
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }
            }

            return Task.FromResult(this.Messaging.LastErrorNumber);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument))
            {
                var parameter = argument.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "data":
                        this.SaveAsData = true;
                        return true;

                    case "o":
                    case "out":
                        this.OutputPath = parser.GetNextArgumentAsFilePathOrError(argument, "output file");
                        return true;

                    case "sct":
                        this.SuppressCustomTables = true;
                        return true;

                    case "sdet":
                        this.SuppressDroppingEmptyTables = true;
                        return true;

                    case "sras":
                        this.SuppressRelativeActionSequencing = true;
                        return true;

                    case "sui":
                        this.SuppressUI = true;
                        return true;

                    case "type":
                        this.DecompileType = parser.GetNextArgumentOrError(argument);
                        return true;

                    case "x":
                        // Peek ahead to get the actual value provided on the command-line so the authoring
                        // matches what they typed on the command-line.
                        var originalExportBasePath = parser.PeekNextArgument();
                        parser.GetNextArgumentAsDirectoryOrError(argument); // ensure we actually got a directory.

                        this.ExportBasePath = originalExportBasePath;
                        return true;
                }
            }
            else if (String.IsNullOrEmpty(this.InputPath))
            {
                this.InputPath = argument;
                return true;
            }

            return false;
        }

        private bool TryCalculateDecompileType(out OutputType decompileType)
        {
            decompileType = OutputType.Unknown;

            if (String.IsNullOrEmpty(this.DecompileType))
            {
                this.DecompileType = Path.GetExtension(this.InputPath);
            }

            switch (this.DecompileType.ToLowerInvariant())
            {
                case "product":
                case "package":
                case "msi":
                case ".msi":
                    decompileType = OutputType.Package;
                    break;

                case "mergemodule":
                case "module":
                case "msm":
                case ".msm":
                    decompileType = OutputType.Module;
                    break;

                case "transform":
                case "mst":
                case ".mst":
                    decompileType = OutputType.Transform;
                    break;
            }

            return decompileType != OutputType.Unknown;
        }

        private string CalculateExtensionFromDecompileType(OutputType decompileType)
        {
            switch (decompileType)
            {
                case OutputType.Package:
                    return this.SaveAsData ? ".wixmsi" : ".wxs";

                case OutputType.Module:
                    return this.SaveAsData ? ".wixmsm" : ".wxs";

                case OutputType.Transform:
                    return ".wixmst";

                default:
                    return this.SaveAsData ? ".wixdata" : ".wxs";
            }
        }
    }
}
