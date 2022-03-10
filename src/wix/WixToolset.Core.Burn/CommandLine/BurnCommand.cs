// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Burn specialized command.
    /// </summary>
    internal class BurnCommand : ICommandLineCommand
    {
        public BurnCommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public bool ShowHelp { get; set; }

        public bool ShowLogo { get; set; }

        public bool StopParsing { get; set; }

        private IServiceProvider ServiceProvider { get; }

        private BurnSubcommandBase Subcommand { get; set; }

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

        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: wix burn detach|reattach bundle.exe -out engine.exe");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h|--help         Show command line help.");
            Console.WriteLine("  --nologo          Suppress displaying the logo information.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine();
            Console.WriteLine("  detach            Detaches the burn engine from a bundle so it can be signed.");
            Console.WriteLine("  reattach          Reattaches a signed burn engine to a bundle.");
        }
    }
}
