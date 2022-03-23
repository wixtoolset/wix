// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// Provides the command-line arguments.
    /// </summary>
    public interface ICommandLineParser
    {
        /// <summary>
        /// Gets the argument that caused the error.
        /// </summary>
        string ErrorArgument { get; }

        /// <summary>
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="argument">The string check.</param>
        /// <returns>True if a valid switch, otherwise false.</returns>
        bool IsSwitch(string argument);

        /// <summary>
        /// Gets the current argument as a file or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <param name="fileType">Type of file displayed in the error message if necessary.</param>
        /// <returns>The fully expanded path if the argument is a file path, otherwise null.</returns>
        string GetArgumentAsFilePathOrError(string argument, string fileType);

        /// <summary>
        /// Adds the current argument as a file to the list or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <param name="fileType">Type of file displayed in the error message if necessary.</param>
        /// <param name="paths">List to add the fully expanded path if the argument is a file path.</param>
        /// <returns>True if the argument is a file path, otherwise false.</returns>
        bool GetArgumentAsFilePathOrError(string argument, string fileType, IList<string> paths);

        /// <summary>
        /// Gets the next argument or displays error if no argument is available.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <returns>The next argument if present or null</returns>
        string GetNextArgumentOrError(string argument);

        /// <summary>
        /// Adds the next argument to a list or displays error if no argument is available.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <param name="arguments">List to add the argument to.</param>
        /// <returns>True if an argument is available, otherwise false.</returns>
        bool GetNextArgumentOrError(string argument, IList<string> arguments);

        /// <summary>
        /// Gets the next argument as a directory or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <returns>The fully expanded path if the argument is a directory, otherwise null.</returns>
        string GetNextArgumentAsDirectoryOrError(string argument);

        /// <summary>
        /// Adds the next argument as a directory to the list or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <param name="directories">List to add the fully expanded directory if the argument is a file path.</param>
        /// <returns>True if the argument is a directory, otherwise false.</returns>
        bool GetNextArgumentAsDirectoryOrError(string argument, IList<string> directories);

        /// <summary>
        /// Gets the next argument as a file or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <returns>The fully expanded path if the argument is a file path, otherwise null.</returns>
        string GetNextArgumentAsFilePathOrError(string argument);

        /// <summary>
        /// Adds the next argument as a file to the list or displays an error.
        /// </summary>
        /// <param name="argument">Current argument used in the error message if necessary.</param>
        /// <param name="fileType">Type of file displayed in the error message if necessary.</param>
        /// <param name="paths">List to add the fully expanded path if the argument is a file path.</param>
        /// <returns>True if the argument is a file path, otherwise false.</returns>
        bool GetNextArgumentAsFilePathOrError(string argument, string fileType, IList<string> paths);

        /// <summary>
        /// Reports a command line error for the provided argument.
        /// </summary>
        /// <param name="argument">Argument that caused the error.</param>
        /// <param name="message">Message to report.</param>
        void ReportErrorArgument(string argument, Message message = null);

        /// <summary>
        /// Tries to get the next argument.
        /// </summary>
        /// <param name="argument">Next argument if available.</param>
        /// <returns>True if argument is available, otherwise false.</returns>
        bool TryGetNextSwitchOrArgument(out string argument);

        /// <summary>
        /// Looks ahead to the next argument without moving to the next argument.
        /// </summary>
        /// <returns>Next argument if available, otherwise null.</returns>
        string PeekNextArgument();

        /// <summary>
        /// Tries to looks ahead to the next argument without moving to the next argument.
        /// </summary>
        /// <param name="argument">Argument found if present.</param>
        /// <returns>True if argument is found, otherwise false.</returns>
        bool TryPeekNextArgument(out string argument);
    }
}
