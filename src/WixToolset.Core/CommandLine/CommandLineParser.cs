// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class CommandLineParser : ICommandLineParser
    {
        private const string ExpectedArgument = "expected argument";

        public string ErrorArgument { get; set; }

        private Queue<string> RemainingArguments { get; }

        private IMessaging Messaging { get; }

        public CommandLineParser(IMessaging messaging, string[] arguments, string errorArgument)
        {
            this.Messaging = messaging;
            this.RemainingArguments = new Queue<string>(arguments);
            this.ErrorArgument = errorArgument;
        }

        public bool IsSwitch(string arg)
        {
            return !String.IsNullOrEmpty(arg) && '-' == arg[0];
        }

        public string GetArgumentAsFilePathOrError(string argument, string fileType)
        {
            if (!File.Exists(argument))
            {
                this.Messaging.Write(ErrorMessages.FileNotFound(null, argument, fileType));
                return null;
            }

            return argument;
        }

        public void GetArgumentAsFilePathOrError(string argument, string fileType, IList<string> paths)
        {
            foreach (var path in this.GetFiles(argument, fileType))
            {
                paths.Add(path);
            }
        }

        public string GetNextArgumentOrError(string commandLineSwitch)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var argument))
            {
                return argument;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return null;
        }

        public bool GetNextArgumentOrError(string commandLineSwitch, IList<string> args)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var arg))
            {
                args.Add(arg);
                return true;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return false;
        }

        public string GetNextArgumentAsDirectoryOrError(string commandLineSwitch)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var arg) && this.TryGetDirectory(commandLineSwitch, arg, out var directory))
            {
                return directory;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return null;
        }

        public bool GetNextArgumentAsDirectoryOrError(string commandLineSwitch, IList<string> directories)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var arg) && this.TryGetDirectory(commandLineSwitch, arg, out var directory))
            {
                directories.Add(directory);
                return true;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return false;
        }

        public string GetNextArgumentAsFilePathOrError(string commandLineSwitch)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var arg) && this.TryGetFile(commandLineSwitch, arg, out var path))
            {
                return path;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return null;
        }

        public bool GetNextArgumentAsFilePathOrError(string commandLineSwitch, string fileType, IList<string> paths)
        {
            if (this.TryGetNextNonSwitchArgumentOrError(out var arg))
            {
                foreach (var path in this.GetFiles(arg, fileType))
                {
                    paths.Add(path);
                }

                return true;
            }

            this.Messaging.Write(ErrorMessages.ExpectedArgument(commandLineSwitch));
            return false;
        }

        public bool TryGetNextSwitchOrArgument(out string arg)
        {
            if (this.RemainingArguments.Count > 0)
            {
                arg = this.RemainingArguments.Dequeue();
                return true;
            }

            arg = null;
            return false;
        }

        private bool TryGetNextNonSwitchArgumentOrError(out string arg)
        {
            var result = this.TryGetNextSwitchOrArgument(out arg);

            if (!result && !this.IsSwitch(arg))
            {
                this.ErrorArgument = arg ?? CommandLineParser.ExpectedArgument;
            }

            return result;
        }

        private bool TryGetDirectory(string commandlineSwitch, string arg, out string directory)
        {
            directory = null;

            if (File.Exists(arg))
            {
                this.Messaging.Write(ErrorMessages.ExpectedDirectoryGotFile(commandlineSwitch, arg));
                return false;
            }

            directory = this.VerifyPath(arg);
            return directory != null;
        }

        private bool TryGetFile(string commandlineSwitch, string arg, out string path)
        {
            path = null;

            if (String.IsNullOrEmpty(arg) || '-' == arg[0])
            {
                this.Messaging.Write(ErrorMessages.FilePathRequired(commandlineSwitch));
            }
            else if (Directory.Exists(arg))
            {
                this.Messaging.Write(ErrorMessages.ExpectedFileGotDirectory(commandlineSwitch, arg));
            }
            else
            {
                path = this.VerifyPath(arg);
            }

            return path != null;
        }

        /// <summary>
        /// Get a set of files that possibly have a search pattern in the path (such as '*').
        /// </summary>
        /// <param name="searchPath">Search path to find files in.</param>
        /// <param name="fileType">Type of file; typically "Source".</param>
        /// <returns>An array of files matching the search path.</returns>
        /// <remarks>
        /// This method is written in this verbose way because it needs to support ".." in the path.
        /// It needs the directory path isolated from the file name in order to use Directory.GetFiles
        /// or DirectoryInfo.GetFiles.  The only way to get this directory path is manually since
        /// Path.GetDirectoryName does not support ".." in the path.
        /// </remarks>
        private string[] GetFiles(string searchPath, string fileType)
        {
            if (null == searchPath)
            {
                throw new ArgumentNullException(nameof(searchPath));
            }

            // Convert alternate directory separators to the standard one.
            var filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            var files = new string[0];

            try
            {
                if (0 > lastSeparator)
                {
                    files = Directory.GetFiles(".", filePath);
                }
                else // found directory separator
                {
                    files = Directory.GetFiles(filePath.Substring(0, lastSeparator + 1), filePath.Substring(lastSeparator + 1));
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Don't let this function throw the DirectoryNotFoundException. This exception
                // occurs for non-existant directories and invalid characters in the searchPattern.
            }
            catch (ArgumentException)
            {
                // Don't let this function throw the ArgumentException. This exception
                // occurs in certain situations such as when passing a malformed UNC path.
            }
            catch (IOException)
            {
            }

            if (0 == files.Length)
            {
                this.Messaging.Write(ErrorMessages.FileNotFound(null, searchPath, fileType));
            }

            return files;
        }

        private string VerifyPath(string path)
        {
            string fullPath;

            if (0 <= path.IndexOf('\"'))
            {
                this.Messaging.Write(ErrorMessages.PathCannotContainQuote(path));
                return null;
            }

            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception e)
            {
                this.Messaging.Write(ErrorMessages.InvalidCommandLineFileName(path, e.Message));
                return null;
            }

            return fullPath;
        }
    }
}
