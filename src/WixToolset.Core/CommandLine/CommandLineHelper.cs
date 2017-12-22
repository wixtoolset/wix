// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public class CommandLineHelper
    {
        /// <summary>
        /// Validates that a string is a valid directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetDirectory(string commandlineSwitch, IMessaging messageHandler, string[] args, int index)
        {
            return GetDirectory(commandlineSwitch, messageHandler, args, index, false);
        }

        /// <summary>
        /// Validates that a string is a valid directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetDirectory(string commandlineSwitch, IMessaging messageHandler, string[] args, int index, bool allowPrefix)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                messageHandler.Write(ErrorMessages.DirectoryPathRequired(commandlineSwitch));
                return null;
            }

            if (File.Exists(args[index]))
            {
                messageHandler.Write(ErrorMessages.ExpectedDirectoryGotFile(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(messageHandler, args[index], allowPrefix);
        }

        /// <summary>
        /// Validates that a string is a valid filename, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetFile(string commandlineSwitch, IMessaging messageHandler, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                messageHandler.Write(ErrorMessages.FilePathRequired(commandlineSwitch));
                return null;
            }

            if (Directory.Exists(args[index]))
            {
                messageHandler.Write(ErrorMessages.ExpectedFileGotDirectory(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(messageHandler, args[index]);
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
        /// <exception cref="WixFileNotFoundException">Throws WixFileNotFoundException if no file matching the pattern can be found.</exception>
        public static string[] GetFiles(string searchPath, string fileType)
        {
            if (null == searchPath)
            {
                throw new ArgumentNullException(nameof(searchPath));
            }

            // Convert alternate directory separators to the standard one.
            string filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            string[] files = null;

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
                throw new WixFileNotFoundException(searchPath, fileType);
            }

            if (null == files || 0 == files.Length)
            {
                throw new WixFileNotFoundException(searchPath, fileType);
            }

            return files;
        }

        /// <summary>
        /// Validates that a valid string parameter (without "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid string parameter exists there, false if not.</returns>
        public static bool IsValidArg(string[] args, int index)
        {
            if (args.Length <= index || String.IsNullOrEmpty(args[index]) || '/' == args[index][0] || '-' == args[index][0])
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="path">The path to test.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(IMessaging messageHandler, string path)
        {
            return VerifyPath(messageHandler, path, false);
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="path">The path to test.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The full path if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(IMessaging messageHandler, string path, bool allowPrefix)
        {
            string fullPath;

            if (0 <= path.IndexOf('\"'))
            {
                messageHandler.Write(ErrorMessages.PathCannotContainQuote(path));
                return null;
            }

            try
            {
                string prefix = null;
                if (allowPrefix)
                {
                    int prefixLength = path.IndexOf('=') + 1;
                    if (0 != prefixLength)
                    {
                        prefix = path.Substring(0, prefixLength);
                        path = path.Substring(prefixLength);
                    }
                }

                if (String.IsNullOrEmpty(prefix))
                {
                    fullPath = Path.GetFullPath(path);
                }
                else
                {
                    fullPath = String.Concat(prefix, Path.GetFullPath(path));
                }
            }
            catch (Exception e)
            {
                messageHandler.Write(ErrorMessages.InvalidCommandLineFileName(path, e.Message));
                return null;
            }

            return fullPath;
        }
    }
}
