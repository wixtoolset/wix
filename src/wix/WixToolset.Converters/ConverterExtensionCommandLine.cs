// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Parses the "convert" command-line command. See <c>ConvertCommand</c> for
    /// the bulk of the command-line processing.
    /// </summary>
    internal class ConverterExtensionCommandLine : BaseExtensionCommandLine
    {
        public ConverterExtensionCommandLine(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public override IReadOnlyCollection<ExtensionCommandLineSwitch> CommandLineSwitches => new ExtensionCommandLineSwitch[]
        {
            new ExtensionCommandLineSwitch { Switch = "convert", Description = "Convert v3 source code to v4 source code." },
            new ExtensionCommandLineSwitch { Switch = "format", Description = "Ensures consistent formatting of source code." },
        };

        public override bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;

            if ("convert".Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                command = new ConvertCommand(this.ServiceProvider);
            }
            else if ("format".Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                command = new FormatCommand(this.ServiceProvider);
            }

            return command != null;
        }
    }
}
