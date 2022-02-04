// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.WindowsInstaller.CommandLine;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Parses the "msi" command-line command. See <c>WindowsInstallerCommand</c>
    /// for the bulk of the command-line processing.
    /// </summary>
    internal class WindowsInstallerExtensionCommandLine : BaseExtensionCommandLine
    {
        public WindowsInstallerExtensionCommandLine(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public override IReadOnlyCollection<ExtensionCommandLineSwitch> CommandLineSwitches => new ExtensionCommandLineSwitch[]
        {
            new ExtensionCommandLineSwitch { Switch = "msi", Description = "Windows Installer specialized operations." },
        };

        public override bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;

            if ("msi".Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                command = new WindowsInstallerCommand(this.ServiceProvider);
            }

            return command != null;
        }
    }
}
