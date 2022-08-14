// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.WindowsInstaller.Inscribe;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class InscribeSubcommand : WindowsInstallerSubcommandBase
    {
        public InscribeSubcommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private string InputPath { get; set; }

        private string IntermediateFolder { get; set; }

        private string OutputPath { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Updates MSI database with cabinet signature information.", "msi inscribe [options] input.msi [-out inscribed.msi]", new[]
            {
                new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified %TMP% will be used."),
                new CommandLineHelpSwitch("-out", "-o", "Path to output the inscribed MSI. If not provided, the input MSI is updated in place."),
            });
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                this.Messaging.Write(ErrorMessages.FilePathRequired("input MSI database"));
            }
            else
            {
                if (String.IsNullOrEmpty(this.IntermediateFolder))
                {
                    this.IntermediateFolder = Path.GetTempPath();
                }

                if (String.IsNullOrEmpty(this.OutputPath))
                {
                    this.OutputPath = this.InputPath;
                }

                var command = new InscribeMsiPackageCommand(this.ServiceProvider, this.InputPath, this.IntermediateFolder, this.OutputPath);
                command.Execute();
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
                        this.OutputPath = parser.GetNextArgumentAsFilePathOrError(argument, "output file");
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
    }
}
