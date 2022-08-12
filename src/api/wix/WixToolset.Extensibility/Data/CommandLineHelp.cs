// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// A command line option (switch or command) description.
    /// </summary>
    public class CommandLineHelp
    {
        /// <summary>
        /// Creates command line help.
        /// </summary>
        /// <param name="description">Description for the command line option.</param>
        /// <param name="usage">Optional usage for the command line option.</param>
        /// <param name="switches">Optional list of switches.</param>
        /// <param name="commands">Optional list of commands.</param>
        public CommandLineHelp(string description, string usage = null, IReadOnlyCollection<CommandLineHelpSwitch> switches = null, IReadOnlyCollection<CommandLineHelpCommand> commands = null)
        {
            this.Description = description;
            this.Usage = usage;
            this.Switches = switches;
            this.Commands = commands;
        }

        /// <summary>
        /// Description for the command line option.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Usage for the command line option.
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// Optional additional notes for the command line option.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Optional list of command line switches.
        /// </summary>
        public IReadOnlyCollection<CommandLineHelpSwitch> Switches { get; set; }

        /// <summary>
        /// Optional list of command line commands.
        /// </summary>
        public IReadOnlyCollection<CommandLineHelpCommand> Commands { get; set; }
    }
}
