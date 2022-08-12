// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// A command line switch description.
    /// </summary>
    public class CommandLineHelpSwitch
    {
        /// <summary>
        /// Creates help for command line switch.
        /// </summary>
        /// <param name="name">Name of switch.</param>
        /// <param name="description">Description for switch.</param>
        public CommandLineHelpSwitch(string name, string description) : this(name, null, description)
        {
        }

        /// <summary>
        /// Creates help for command line switch.
        /// </summary>
        /// <param name="name">Name of switch.</param>
        /// <param name="shortName">Optional short name of switch.</param>
        /// <param name="description">Description for switch.</param>
        public CommandLineHelpSwitch(string name, string shortName, string description)
        {
            Name = name;
            ShortName = shortName;
            Description = description;
        }

        /// <summary>
        /// Name for switch.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional short name for switch.
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Description of the switch.
        /// </summary>
        public string Description { get; set; }
    }
}
