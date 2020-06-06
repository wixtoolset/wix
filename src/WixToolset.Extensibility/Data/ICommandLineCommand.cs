// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Custom command.
    /// </summary>
    public interface ICommandLineCommand
    {
        /// <summary>
        /// Indicates the command-line should show the command-line logo.
        /// </summary>
        bool ShowLogo { get; }

        /// <summary>
        /// Indicates the command-line parsing can stop.
        /// </summary>
        bool StopParsing { get; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <returns>Exit code for the command.</returns>
        int Execute();

        /// <summary>
        /// Allows the command to parse command-line arguments.
        /// </summary>
        /// <param name="parser">Parser to help parse the argument and additional arguments.</param>
        /// <param name="argument">Argument to parse.</param>
        /// <returns>True if the argument is recognized; otherwise false to allow another extension to process it.</returns>
        bool TryParseArgument(ICommandLineParser parser, string argument); 
    }
}
