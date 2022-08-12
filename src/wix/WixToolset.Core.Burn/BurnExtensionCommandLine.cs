// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using WixToolset.Core.Burn.CommandLine;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Parses the "burn" command-line command. See <c>BurnCommand</c>
    /// for the bulk of the command-line processing.
    /// </summary>
    internal class BurnExtensionCommandLine : BaseExtensionCommandLine
    {
        public BurnExtensionCommandLine(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp(null)
            {
                Commands = new[] { new CommandLineHelpCommand("burn", "Specialized operations for manipulating Burn-based bundles.") }
            };
        }

        public override bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;

            if ("burn".Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                command = new BurnCommand(this.ServiceProvider);
            }

            return command != null;
        }
    }
}
