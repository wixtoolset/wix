// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Parsed command-line arguments.
    /// </summary>
    public interface ICommandLineArguments
    {
#pragma warning disable 1591 // TODO: add documentation
        string[] OriginalArguments { get; set; }

        string[] Arguments { get; set; }

        string[] Extensions { get; set; }

        string ErrorArgument { get; set; }

        /// <summary>
        /// Populate this argument from a string.
        /// </summary>
        /// <param name="commandLine">String to parse.</param>
        void Populate(string commandLine);

        /// <summary>
        /// Populate this argument from array of strings.
        /// </summary>
        /// <param name="args">Array of strings.</param>
        void Populate(string[] args);

        /// <summary>
        /// Parses this arguments after it is populated.
        /// </summary>
        /// <returns>Parser for this arguments.</returns>
        ICommandLineParser Parse();
    }
}
