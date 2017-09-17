// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixToolset.Extensibility;

    internal class X_CommandLine
    {
        private X_CommandLine()
        {
        }

        public static string ExpectedArgument { get; } = "expected argument";

        public string ActiveCommand { get; private set; }

        public string[] OriginalArguments { get; private set; }

        public Queue<string> RemainingArguments { get; } = new Queue<string>();

        public ExtensionManager ExtensionManager { get; } = new ExtensionManager();

        public string ErrorArgument { get; set; }

        public bool ShowHelp { get; set; }

        public static X_CommandLine Parse(string commandLineString, Func<X_CommandLine, string, bool> parseArgument)
        {
            var arguments = X_CommandLine.ParseArgumentsToArray(commandLineString).ToArray();

            return X_CommandLine.Parse(arguments, null, parseArgument);
        }

        public static X_CommandLine Parse(string[] commandLineArguments, Func<X_CommandLine, string, bool> parseArgument)
        {
            return X_CommandLine.Parse(commandLineArguments, null, parseArgument);
        }

        public static X_CommandLine Parse(string[] commandLineArguments, Func<X_CommandLine, string, bool> parseCommand, Func<X_CommandLine, string, bool> parseArgument)
        {
            var cmdline = new X_CommandLine();

            cmdline.FlattenArgumentsWithResponseFilesIntoOriginalArguments(commandLineArguments);

            cmdline.QueueArgumentsAndLoadExtensions(cmdline.OriginalArguments);

            cmdline.ProcessRemainingArguments(parseArgument, parseCommand);

            return cmdline;
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
        public string[] GetFiles(string searchPath, string fileType)
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
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid switch exists there, false if not.</returns>
        public bool IsSwitch(string arg)
        {
            return arg != null && ('/' == arg[0] || '-' == arg[0]);
        }

        /// <summary>
        /// Validates that a valid switch (starts with "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid switch exists there, false if not.</returns>
        public bool IsSwitchAt(IEnumerable<string> args, int index)
        {
            var arg = args.ElementAtOrDefault(index);
            return IsSwitch(arg);
        }

        public void GetNextArgumentOrError(ref string arg)
        {
            this.TryGetNextArgumentOrError(out arg);
        }

        public void GetNextArgumentOrError(IList<string> args)
        {
            if (this.TryGetNextArgumentOrError(out var arg))
            {
                args.Add(arg);
            }
        }

        public void GetNextArgumentAsFilePathOrError(IList<string> args, string fileType)
        {
            if (this.TryGetNextArgumentOrError(out var arg))
            {
                foreach (var path in this.GetFiles(arg, fileType))
                {
                    args.Add(path);
                }
            }
        }

        public bool TryGetNextArgumentOrError(out string arg)
        {
            if (this.RemainingArguments.TryDequeue(out arg) && !this.IsSwitch(arg))
            {
                return true;
            }

            this.ErrorArgument = arg ?? X_CommandLine.ExpectedArgument;

            return false;
        }

        private void FlattenArgumentsWithResponseFilesIntoOriginalArguments(string[] commandLineArguments)
        {
            List<string> args = new List<string>();

            foreach (var arg in commandLineArguments)
            {
                if ('@' == arg[0])
                {
                    var responseFileArguments = X_CommandLine.ParseResponseFile(arg.Substring(1));
                    args.AddRange(responseFileArguments);
                }
                else
                {
                    args.Add(arg);
                }
            }

            this.OriginalArguments = args.ToArray();
        }

        private void QueueArgumentsAndLoadExtensions(string[] args)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];

                if ("-ext" == arg || "/ext" == arg)
                {
                    if (!this.IsSwitchAt(args, ++i))
                    {
                        this.ExtensionManager.Load(args[i]);
                    }
                    else
                    {
                        this.ErrorArgument = arg;
                        break;
                    }
                }
                else
                {
                    this.RemainingArguments.Enqueue(arg);
                }
            }
        }

        private void ProcessRemainingArguments(Func<X_CommandLine, string, bool> parseArgument, Func<X_CommandLine, string, bool> parseCommand)
        {
            var extensions = this.ExtensionManager.Create<IExtensionCommandLine>();

            while (!this.ShowHelp &&
                   String.IsNullOrEmpty(this.ErrorArgument) &&
                   this.RemainingArguments.TryDequeue(out var arg))
            {
                if (String.IsNullOrWhiteSpace(arg)) // skip blank arguments.
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    if (!parseArgument(this, arg) &&
                        !this.TryParseCommandLineArgumentWithExtension(arg, extensions))
                    {
                        this.ErrorArgument = arg;
                    }
                }
                else if (String.IsNullOrEmpty(this.ActiveCommand) && parseCommand != null) // First non-switch must be the command, if commands are supported.
                {
                    if (parseCommand(this, arg))
                    {
                        this.ActiveCommand = arg;
                    }
                    else
                    {
                        this.ErrorArgument = arg;
                    }
                }
                else if (!this.TryParseCommandLineArgumentWithExtension(arg, extensions) &&
                         !parseArgument(this, arg))
                {
                    this.ErrorArgument = arg;
                }
            }
        }

        private bool TryParseCommandLineArgumentWithExtension(string arg, IEnumerable<IExtensionCommandLine> extensions)
        {
            foreach (var extension in extensions)
            {
                //if (extension.ParseArgument(this, arg))
                //{
                //    return true;
                //}
            }

            return false;
        }

        /// <summary>
        /// Parses a response file.
        /// </summary>
        /// <param name="responseFile">The file to parse.</param>
        /// <returns>The array of arguments.</returns>
        private static List<string> ParseResponseFile(string responseFile)
        {
            string arguments;

            using (StreamReader reader = new StreamReader(responseFile))
            {
                arguments = reader.ReadToEnd();
            }

            return X_CommandLine.ParseArgumentsToArray(arguments);
        }

        /// <summary>
        /// Parses an argument string into an argument array based on whitespace and quoting.
        /// </summary>
        /// <param name="arguments">Argument string.</param>
        /// <returns>Argument array.</returns>
        private static List<string> ParseArgumentsToArray(string arguments)
        {
            // Scan and parse the arguments string, dividing up the arguments based on whitespace.
            // Unescaped quotes cause whitespace to be ignored, while the quotes themselves are removed.
            // Quotes may begin and end inside arguments; they don't necessarily just surround whole arguments.
            // Escaped quotes and escaped backslashes also need to be unescaped by this process.

            // Collects the final list of arguments to be returned.
            var argsList = new List<string>();

            // True if we are inside an unescaped quote, meaning whitespace should be ignored.
            var insideQuote = false;

            // Index of the start of the current argument substring; either the start of the argument
            // or the start of a quoted or unquoted sequence within it.
            var partStart = 0;

            // The current argument string being built; when completed it will be added to the list.
            var arg = new StringBuilder();

            for (int i = 0; i <= arguments.Length; i++)
            {
                if (i == arguments.Length || (Char.IsWhiteSpace(arguments[i]) && !insideQuote))
                {
                    // Reached a whitespace separator or the end of the string.

                    // Finish building the current argument.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // Skip over the whitespace character.
                    partStart = i + 1;

                    // Add the argument to the list if it's not empty.
                    if (arg.Length > 0)
                    {
                        argsList.Add(X_CommandLine.ExpandEnvVars(arg.ToString()));
                        arg.Length = 0;
                    }
                }
                else if (i > partStart && arguments[i - 1] == '\\')
                {
                    // Check the character following an unprocessed backslash.
                    // Unescape quotes, and backslashes followed by a quote.
                    if (arguments[i] == '"' || (arguments[i] == '\\' && arguments.Length > i + 1 && arguments[i + 1] == '"'))
                    {
                        // Unescape the quote or backslash by skipping the preceeding backslash.
                        arg.Append(arguments.Substring(partStart, i - 1 - partStart));
                        arg.Append(arguments[i]);
                        partStart = i + 1;
                    }
                }
                else if (arguments[i] == '"')
                {
                    // Add the quoted or unquoted section to the argument string.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // And skip over the quote character.
                    partStart = i + 1;

                    insideQuote = !insideQuote;
                }
            }

            return argsList;
        }

        /// <summary>
        /// Expand enxironment variables contained in the passed string
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static string ExpandEnvVars(string arguments)
        {
            var id = Environment.GetEnvironmentVariables();

            var regex = new Regex("(?<=\\%)(?:[\\w\\.]+)(?=\\%)");
            MatchCollection matches = regex.Matches(arguments);

            string value = String.Empty;
            for (int i = 0; i <= (matches.Count - 1); i++)
            {
                try
                {
                    var key = matches[i].Value;
                    regex = new Regex(String.Concat("(?i)(?:\\%)(?:", key, ")(?:\\%)"));
                    value = id[key].ToString();
                    arguments = regex.Replace(arguments, value);
                }
                catch (NullReferenceException)
                {
                    // Collapse unresolved environment variables.
                    arguments = regex.Replace(arguments, value);
                }
            }

            return arguments;
        }
    }
}
