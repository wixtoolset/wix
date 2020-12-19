// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;

#pragma warning disable 1591 // TODO: add documentation
    public interface ICommandLineParser
    {
        string ErrorArgument { get; set; }

        /// <summary>
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="arg">The string check.</param>
        /// <returns>True if a valid switch, otherwise false.</returns>
        bool IsSwitch(string arg);

        string GetArgumentAsFilePathOrError(string argument, string fileType);

        void GetArgumentAsFilePathOrError(string argument, string fileType, IList<string> paths);

        string GetNextArgumentOrError(string commandLineSwitch);

        bool GetNextArgumentOrError(string commandLineSwitch, IList<string> argument);

        string GetNextArgumentAsDirectoryOrError(string commandLineSwitch);

        bool GetNextArgumentAsDirectoryOrError(string commandLineSwitch, IList<string> directories);

        string GetNextArgumentAsFilePathOrError(string commandLineSwitch);

        bool GetNextArgumentAsFilePathOrError(string commandLineSwitch, string fileType, IList<string> paths);

        bool TryGetNextSwitchOrArgument(out string arg);
    }
}
