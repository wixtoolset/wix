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
        public ConverterExtensionCommandLine(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        // TODO: Do something with CommandLineSwitches
        public override IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches => base.CommandLineSwitches;

        public override bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;

            if ("convert".Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                command = new ConvertCommand(this.ServiceProvider);
            }

            return command != null;
        }
    }
}
