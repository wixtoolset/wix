// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Windows Installer specialized command.
    /// </summary>
    internal class WindowsInstallerCommand : BaseCommandLineCommand
    {
        public WindowsInstallerCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        private WindowsInstallerSubcommandBase Subcommand { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return this.Subcommand?.GetCommandLineHelp() ?? new CommandLineHelp("Specialized operations for manipulating Windows Installer databases.", "msi decompile|inscribe|transform|validate")
            {
                Commands = new[]
                {
                    new CommandLineHelpCommand("decompile", "Converts a Windows Installer database back into source code."),
                    new CommandLineHelpCommand("inscribe", "Updates MSI database with cabinet signature information."),
                    new CommandLineHelpCommand("transform", "Creates an MST transform file."),
                    new CommandLineHelpCommand("validate", "Validates MSI database using standard or custom ICEs."),
                }
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.Subcommand is null)
            {
                Console.Error.WriteLine("A subcommand is required for the \"msi\" command. Add -h to for help.");
                return Task.FromResult(1);
            }

            return this.Subcommand.ExecuteAsync(cancellationToken);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (this.Subcommand is null)
            {
                switch (argument.ToLowerInvariant())
                {
                    case "decompile":
                        this.Subcommand = new DecompilerSubcommand(this.ServiceProvider);
                        return true;

                    case "inscribe":
                        this.Subcommand = new InscribeSubcommand(this.ServiceProvider);
                        return true;

                    case "transform":
                        this.Subcommand = new TransformSubcommand(this.ServiceProvider);
                        return true;

                    case "validate":
                        this.Subcommand = new ValidateSubcommand(this.ServiceProvider);
                        return true;
                }

                return false;
            }

            return this.Subcommand.TryParseArgument(parser, argument);
        }
    }
}
