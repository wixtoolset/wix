// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Burn specialized command.
    /// </summary>
    internal class BurnCommand : BaseCommandLineCommand
    {
        public BurnCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        private BurnSubcommandBase Subcommand { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return this.Subcommand?.GetCommandLineHelp() ?? new CommandLineHelp("Specialized operations for manipulating Burn-based bundles.", "burn detach|extract|reattach|remotepayload")
            {
                Commands = new[]
                {
                    new CommandLineHelpCommand("detach", "Detaches the burn engine from a bundle so it can be signed."),
                    new CommandLineHelpCommand("extract", "Extracts the internals of a bundle to a folder."),
                    new CommandLineHelpCommand("reattach", "Reattaches a signed burn engine to a bundle."),
                    new CommandLineHelpCommand("remotepayload", "Extracts the internals of a bundle."),
                }
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.Subcommand is null)
            {
                Console.Error.WriteLine("A subcommand is required for the \"burn\" command. Add -h to for help.");
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
                    case "detach":
                        this.Subcommand = new DetachSubcommand(this.ServiceProvider);
                        return true;

                    case "extract":
                        this.Subcommand = new ExtractSubcommand(this.ServiceProvider);
                        return true;

                    case "reattach":
                        this.Subcommand = new ReattachSubcommand(this.ServiceProvider);
                        return true;

                    case "remotepayload":
                        this.Subcommand = new RemotePayloadSubcommand(this.ServiceProvider);
                        return true;
                }

                return false;
            }

            return this.Subcommand.TryParseArgument(parser, argument);
        }
    }
}
