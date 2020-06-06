// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Interface extensions implement to be able to parse the command-line.
    /// </summary>
    public interface IExtensionCommandLine
    {
        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches { get; }

        /// <summary>
        /// Called before the command-line is parsed.
        /// </summary>
        /// <param name="context">Information about the command-line to be parsed.</param>
        void PreParse(ICommandLineContext context);

        /// <summary>
        /// Gives the extension an opportunity pass a command-line argument for another command.
        /// </summary>
        /// <param name="parser">Parser to help parse the argument and additional arguments.</param>
        /// <param name="argument">Argument to parse.</param>
        /// <returns>True if the argument is recognized; otherwise false to allow another extension to process it.</returns>
        bool TryParseArgument(ICommandLineParser parser, string argument);

        /// <summary>
        /// Gives the extension an opportunity to provide a command.
        /// </summary>
        /// </summary>
        /// <param name="parser">Parser to help parse the argument and additional arguments.</param>
        /// <param name="argument">Argument to parse.</param>
        /// <param name="command"></param>
        /// <returns>True if the argument is recognized as a commond; otherwise false to allow another extension to process it.</returns>
        bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command);

        /// <summary>
        /// Called after the command-line is parsed.
        /// </summary>
        void PostParse();
    }
}
