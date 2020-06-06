// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal enum CommandTypes
    {
        Unknown,
        Build,
        Preprocess,
        Compile,
        Link,
        Bind,
        Decompile,
    }

    internal class CommandLine : ICommandLine
    {
        public CommandLine(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; set; }

        public IExtensionManager ExtensionManager { get; set; }

        public ICommandLineArguments Arguments { get; set; }

        public static string ExpectedArgument { get; } = "expected argument";

        public bool ShowHelp { get; private set; }

        public ICommandLineCommand ParseStandardCommandLine()
        {
            var context = this.ServiceProvider.GetService<ICommandLineContext>();
            context.ExtensionManager = this.ExtensionManager ?? this.ServiceProvider.GetService<IExtensionManager>();
            context.Arguments = this.Arguments;

            var command = this.Parse(context);

            if (command.ShowLogo)
            {
                AppCommon.DisplayToolHeader();
            }

            return command;
        }

        private ICommandLineCommand Parse(ICommandLineContext context)
        {
            var extensions = this.ExtensionManager.GetServices<IExtensionCommandLine>();

            foreach (var extension in extensions)
            {
                extension.PreParse(context);
            }

            ICommandLineCommand command = null;
            var parser = context.Arguments.Parse();

            while (command?.StopParsing != true &&
                   String.IsNullOrEmpty(parser.ErrorArgument) &&
                   parser.TryGetNextSwitchOrArgument(out var arg))
            {
                if (String.IsNullOrWhiteSpace(arg)) // skip blank arguments.
                {
                    continue;
                }

                // First argument must be the command or global switch (that creates a command).
                if (command == null)
                {
                    if (!this.TryParseCommand(arg, parser, out command, extensions))
                    {
                        parser.ErrorArgument = arg;
                    }
                }
                else if (parser.IsSwitch(arg))
                {
                    if (!command.TryParseArgument(parser, arg) && !TryParseCommandLineArgumentWithExtension(arg, parser, extensions))
                    {
                        parser.ErrorArgument = arg;
                    }
                }
                else if (!TryParseCommandLineArgumentWithExtension(arg, parser, extensions) && !command.TryParseArgument(parser, arg))
                {
                    parser.ErrorArgument = arg;
                }
            }

            foreach (var extension in extensions)
            {
                extension.PostParse();
            }

            return command ?? new HelpCommand();
        }
        
        private bool TryParseCommand(string arg, ICommandLineParser parser, out ICommandLineCommand command, IEnumerable<IExtensionCommandLine> extensions)
        {
            command = null;

            if (parser.IsSwitch(arg))
            {
                var parameter = arg.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                case "?":
                case "h":
                case "help":
                case "-help":
                    command = new HelpCommand();
                    break;

                case "version":
                case "-version":
                    command = new VersionCommand();
                    break;
                }
            }
            else
            {
                if (Enum.TryParse(arg, true, out CommandTypes commandType))
                {
                    switch (commandType)
                    {
                    case CommandTypes.Build:
                        command = new BuildCommand(this.ServiceProvider);
                        break;

                    case CommandTypes.Compile:
                        command = new CompileCommand(this.ServiceProvider);
                        break;

                    case CommandTypes.Decompile:
                        command = new DecompileCommand(this.ServiceProvider);
                        break;
                    }
                }
                else
                {
                    foreach (var extension in extensions)
                    {
                        if (extension.TryParseCommand(parser, arg, out command))
                        {
                            break;
                        }

                        command = null;
                    }
                }
            }

            return command != null;
        }

        private static bool TryParseCommandLineArgumentWithExtension(string arg, ICommandLineParser parse, IEnumerable<IExtensionCommandLine> extensions)
        {
            foreach (var extension in extensions)
            {
                if (extension.TryParseArgument(parse, arg))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
