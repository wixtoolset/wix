// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CommandLineArguments : ICommandLineArguments
    {
        public CommandLineArguments(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        public string[] OriginalArguments { get; set; }

        public string[] Arguments { get; set; }

        public string[] Extensions { get; set; }

        public string ErrorArgument { get; set; }

        private IMessaging Messaging { get; }

        public void Populate(string commandLine)
        {
            var args = CommandLineArguments.ParseArgumentsToArray(commandLine);

            this.Populate(args.ToArray());
        }

        public void Populate(string[] args)
        {
            this.FlattenArgumentsWithResponseFilesIntoOriginalArguments(args);

            this.ProcessArgumentsAndParseExtensions(this.OriginalArguments);
        }

        public ICommandLineParser Parse() => new CommandLineParser(this.Messaging, this.Arguments, this.ErrorArgument);

        private void FlattenArgumentsWithResponseFilesIntoOriginalArguments(string[] commandLineArguments)
        {
            var args = new List<string>();

            foreach (var arg in commandLineArguments)
            {
                if (arg != null)
                {
                    if ('@' == arg[0])
                    {
                        var responseFileArguments = CommandLineArguments.ParseResponseFile(arg.Substring(1));
                        args.AddRange(responseFileArguments);
                    }
                    else
                    {
                        args.Add(arg);
                    }
                }
            }

            this.OriginalArguments = args.ToArray();
        }

        private void ProcessArgumentsAndParseExtensions(string[] args)
        {
            var arguments = new List<string>();
            var extensions = new List<string>();

            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];

                if ("-ext" == arg || "/ext" == arg)
                {
                    if (!CommandLineArguments.IsSwitchAt(args, ++i))
                    {
                        extensions.Add(args[i]);
                    }
                    else
                    {
                        this.ErrorArgument = arg;
                        break;
                    }
                }
                else
                {
                    arguments.Add(arg);
                }
            }

            this.Arguments = arguments.ToArray();
            this.Extensions = extensions.ToArray();
        }

        private static List<string> ParseResponseFile(string responseFile)
        {
            string arguments;

            using (var reader = new StreamReader(responseFile))
            {
                arguments = reader.ReadToEnd();
            }

            return CommandLineArguments.ParseArgumentsToArray(arguments);
        }

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

            for (var i = 0; i <= arguments.Length; i++)
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
                        argsList.Add(CommandLineArguments.ExpandEnvironmentVariables(arg.ToString()));
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

        private static string ExpandEnvironmentVariables(string arguments)
        {
            var id = Environment.GetEnvironmentVariables();

            var regex = new Regex("(?<=\\%)(?:[\\w\\.]+)(?=\\%)");
            var matches = regex.Matches(arguments);

            var value = String.Empty;
            for (var i = 0; i <= (matches.Count - 1); i++)
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

        private static bool IsSwitchAt(string[] args, int index) => args.Length > index && !String.IsNullOrEmpty(args[index]) && ('/' == args[index][0] || '-' == args[index][0]);
    }
}
