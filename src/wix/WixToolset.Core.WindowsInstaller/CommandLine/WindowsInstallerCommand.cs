// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Windows Installer specialized command.
    /// </summary>
    internal class WindowsInstallerCommand : ICommandLineCommand
    {
        public WindowsInstallerCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public bool ShowHelp { get; set; }

        public bool ShowLogo { get; set; }

        public bool StopParsing { get; set; }

        private IServiceProvider ServiceProvider { get; }

        private WindowsInstallerSubcommandBase Subcommand { get; set; }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.ShowHelp || this.Subcommand is null)
            {
                DisplayHelp();
                return Task.FromResult(1);
            }

            return this.Subcommand.ExecuteAsync(cancellationToken);
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
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

        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: wix msi inscribe input.msi [-intermedidateFolder folder] [-out output.msi]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h|--help         Show command line help.");
            Console.WriteLine("  --nologo          Suppress displaying the logo information.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine();
            Console.WriteLine("  inscribe          Updates MSI database with cabinet signature information.");
            Console.WriteLine("  transform         Creates an MST transform file.");
            Console.WriteLine("  validate          Validates MSI database using standard or custom ICEs.");
        }
    }
}
