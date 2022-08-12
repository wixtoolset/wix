// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.WindowsInstaller.Validate;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ValidateSubcommand : WindowsInstallerSubcommandBase
    {
        public ValidateSubcommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private string DatabasePath { get; set; }

        private string WixpdbPath { get; set; }

        private string IntermediateFolder { get; set; }

        private List<string> CubeFiles { get; } = new List<string>();

        private List<string> Ices { get; } = new List<string>();

        private List<string> SuppressIces { get; } = new List<string>();

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Validates MSI database using standard or custom ICEs.", "msi validate [options] inputfile", new[]
            {
                new CommandLineHelpSwitch("-cub", "Optional path to a custom validation .CUBe file."),
                new CommandLineHelpSwitch("-ice", "Validates only with the specified ICE. May be provided multiple times."),
                new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified %TMP% will be used."),
                new CommandLineHelpSwitch("-pdb", "Optional path to .wixpdb for source line information. If not provided, will check next to the input file."),
                new CommandLineHelpSwitch("-sice", "Suppresses an ICE validator."),
            });
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            WindowsInstallerData data = null;

            if (String.IsNullOrEmpty(this.DatabasePath))
            {
                Console.Error.WriteLine("Input MSI or MSM database is required");
                return Task.FromResult(-1);
            }

            if (this.CubeFiles.Count == 0)
            {
                var ext = Path.GetExtension(this.DatabasePath);
                switch (ext.ToLowerInvariant())
                {
                    case ".msi":
                        this.CubeFiles.Add("darice.cub");
                        break;

                    case ".msm":
                        this.CubeFiles.Add("mergemod.cub");
                        break;

                    default:
                        Console.Error.WriteLine("Unknown extension: {0}. Use the -cub switch to specify the path to the ICE CUBe file", ext);
                        return Task.FromResult(-1);
                }
            }

            if (String.IsNullOrEmpty(this.WixpdbPath))
            {
                this.WixpdbPath = Path.ChangeExtension(this.DatabasePath, ".wixpdb");
            }

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            if (File.Exists(this.WixpdbPath))
            {
                data = WindowsInstallerData.Load(this.WixpdbPath);
            }

            var command = new ValidateDatabaseCommand(this.Messaging, this.FileSystem, this.IntermediateFolder, this.DatabasePath, data, this.CubeFiles, this.Ices, this.SuppressIces);
            command.Execute();

            return Task.FromResult(this.Messaging.EncounteredError ? 1 : 0);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument))
            {
                var parameter = argument.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                    case "cub":
                    {
                        var value = parser.GetNextArgumentOrError(argument);
                        this.CubeFiles.Add(value);
                        return true;
                    }

                    case "ice":
                    {
                        var value = parser.GetNextArgumentOrError(argument);
                        this.Ices.Add(value);
                        return true;
                    }

                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "pdb":
                        this.WixpdbPath = parser.GetNextArgumentAsFilePathOrError(argument);
                        return true;

                    case "sice":
                    {
                        var value = parser.GetNextArgumentOrError(argument);
                        this.SuppressIces.Add(value);
                        return true;
                    }
                }
            }
            else if (String.IsNullOrEmpty(this.DatabasePath))
            {
                this.DatabasePath = argument;
                return true;
            }

            return false;
        }
    }
}
