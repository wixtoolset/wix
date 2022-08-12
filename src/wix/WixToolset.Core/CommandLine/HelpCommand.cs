// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class HelpCommand : BaseCommandLineCommand
    {
        public HelpCommand(IEnumerable<IExtensionCommandLine> extensions, IWixBranding branding, ICommandLineCommand command)
        {
            this.Extensions = extensions;
            this.Branding = branding;
            this.Command = command;
        }

        public override bool ShowLogo => true;

        private IEnumerable<IExtensionCommandLine> Extensions { get; }

        private IWixBranding Branding { get; }

        private ICommandLineCommand Command { get; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp(null, "[command] [options]")
            {
                Switches = new[]
                {
                    new CommandLineHelpSwitch("--help", "-h", "Show command line help."),
                    new CommandLineHelpSwitch("--version", "-v", "Display WiX Toolset version in use."),
                    new CommandLineHelpSwitch("--nologo", "Suppress displaying the logo information."),
                },
                Commands = new[]
                {
                    new CommandLineHelpCommand("build", "Build a wixlib, package or bundle.")
                },
                Notes = "Run 'wix [command] -h' for more information on a command."
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken _)
        {
            var extensionsHelp = this.Extensions.Select(e => e.GetCommandLineHelp()).Where(h => h != null).ToList();

            var help = this.Command?.GetCommandLineHelp() ?? this.GetCommandLineHelp();

            var switches = new List<CommandLineHelpSwitch>();
            if (help.Switches != null)
            {
                switches.AddRange(help.Switches);
            }

            switches.AddRange(extensionsHelp.Where(e => e.Switches != null).SelectMany(e => e.Switches));

            var commands = new List<CommandLineHelpCommand>();
            if (help.Commands != null)
            {
                commands.AddRange(help.Commands);
            }

            if (this.Command == null)
            {
                commands.AddRange(extensionsHelp.Where(e => e.Commands != null).SelectMany(e => e.Commands));
            }

            if (!String.IsNullOrEmpty(help.Description))
            {
                Console.WriteLine("Description:");
                Console.WriteLine("  {0}", help.Description);
                Console.WriteLine();
            }

            if (!String.IsNullOrEmpty(help.Usage))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  wix {0}", help.Usage);
                Console.WriteLine();
            }

            if (switches.Count > 0)
            {
                Console.WriteLine("Options:");
                foreach (var commandLineSwitch in help.Switches)
                {
                    var switchName = String.IsNullOrEmpty(commandLineSwitch.ShortName) ? commandLineSwitch.Name : $"{commandLineSwitch.ShortName}|{commandLineSwitch.Name}";
                    Console.WriteLine("  {0,-17} {1}", switchName, commandLineSwitch.Description);
                }

                Console.WriteLine();
            }

            if (commands.Count > 0)
            {
                Console.WriteLine("Commands:");
                foreach (var command in commands)
                {
                    Console.WriteLine("  {0,-17} {1}", command.Name, command.Description);
                }

                Console.WriteLine();
            }

            if (!String.IsNullOrEmpty(help.Notes))
            {
                Console.WriteLine(help.Notes);
                Console.WriteLine();
            }

            Console.WriteLine(this.Branding.ReplacePlaceholders("For more information see: [SupportUrl]"));

            return Task.FromResult(-1);
        }

        public override bool TryParseArgument(ICommandLineParser parseHelper, string argument)
        {
            return true; // eat any arguments
        }
    }
}
