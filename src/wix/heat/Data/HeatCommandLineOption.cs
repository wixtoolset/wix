// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Data
{
    /// <summary>
    /// A command line option.
    /// </summary>
    public struct HeatCommandLineOption
    {
        /// <summary>
        /// The option name used on the command line.
        /// </summary>
        public string Option;

        /// <summary>
        /// Description shown in Help command.
        /// </summary>
        public string Description;

        /// <summary>
        /// Instantiates a new CommandLineOption.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <param name="description">The description of the option.</param>
        public HeatCommandLineOption(string option, string description)
        {
            this.Option = option;
            this.Description = description;
        }
    }
}
