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

        private bool SuppressCustomTables { get; set; }

        private bool SuppressDroppingEmptyTables { get; set; }

        private bool SuppressRelativeActionSequencing { get; set; }

        private bool SuppressUI { get; set; }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                Console.Error.WriteLine("Input MSI or MSM database is required");
                return Task.FromResult(-1);
            }

            if (!this.TryCalculateDecompileType(out var decompileType))
            {
                Console.Error.WriteLine("Unknown output type '{0}' from input: {1}", decompileType, this.InputPath);
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            if (String.IsNullOrEmpty(this.OutputPath))
            {
                this.OutputPath = Path.ChangeExtension(this.InputPath, ".wxs");
            }

            var context = this.ServiceProvider.GetService<IWindowsInstallerDecompileContext>();
            context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().GetServices<IWindowsInstallerDecompilerExtension>();
            context.DecompilePath = this.InputPath;
            context.DecompileType = decompileType;
            context.IntermediateFolder = this.IntermediateFolder;
            context.OutputPath = this.OutputPath;

            context.ExtractFolder = this.ExportBasePath ?? this.IntermediateFolder;
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
                    result.Document.Save(context.OutputPath, SaveOptions.OmitDuplicateNamespaces);
                }
            }
            catch (WixException e)
            {
                this.Messaging.Write(e.Error);
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

                    case "o":
                    case "out":
                        this.OutputPath = parser.GetNextArgumentAsFilePathOrError(argument);
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
                    decompileType = OutputType.Product;
                    break;

                case "mergemodule":
                case "module":
                case "msm":
                case ".msm":
                    decompileType = OutputType.Module;
                    break;
            }

            return decompileType != OutputType.Unknown;
        }
    }
}
