// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// A command line command description.
    /// </summary>
    public class CommandLineHelpCommand
    {
        /// <summary>
        /// Creates help for command line command.
        /// </summary>
        /// <param name="name">Name of command.</param>
        /// <param name="description">Description for command.</param>
        public CommandLineHelpCommand(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Name of command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the command.
        /// </summary>
        public string Description { get; set; }
    }
}
