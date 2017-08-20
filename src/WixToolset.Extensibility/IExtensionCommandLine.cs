// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// A command line option.
    /// </summary>
    public struct ExtensionCommandLineSwitch
    {
        public string Switch { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// Interface extensions implement to be able to parse command-line options.
    /// </summary>
    public interface IExtensionCommandLine
    {
        /// <summary>
        /// Sets the message handler for the extension.
        /// </summary>
        /// <value>Message handler for the extension.</value>
        IMessageHandler MessageHandler { set; }

        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches { get; }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        /// <returns>Unparsed commandline arguments.</returns>
        string[] ParseCommandLine(string[] args);
    }
}
