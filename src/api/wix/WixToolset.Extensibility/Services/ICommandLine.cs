// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Command-line parsing mechanism.
    /// </summary>
    public interface ICommandLine
    {
        /// <summary>
        /// Simple way to parse arguments and create a command.
        /// </summary>
        /// <param name="args">Unparsed arguments.</param>
        /// <returns>Command if the command-line arguments can be parsed, otherwise null.</returns>
        ICommandLineCommand CreateCommand(string[] args);

        /// <summary>
        /// Simple way to parse arguments and create a command.
        /// </summary>
        /// <param name="commandLine">Unparsed arguments.</param>
        /// <returns>Command if the command-line arguments can be parsed, otherwise null.</returns>
        ICommandLineCommand CreateCommand(string commandLine);

        /// <summary>
        /// Creates a command from populated arguments.
        /// </summary>
        /// <param name="arguments">Parsed arguments.</param>
        /// <returns>Command if the command-line arguments can be parsed, otherwise null.</returns>
        ICommandLineCommand ParseStandardCommandLine(ICommandLineArguments arguments);
    }
}
