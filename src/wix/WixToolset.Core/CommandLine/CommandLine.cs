// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CommandLine : ICommandLine
    {
        public CommandLine(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private bool ShowHelp { get; set; }

        private bool SuppressLogo { get; set; }

        public ICommandLineCommand CreateCommand(string[] args)
        {
            var arguments = this.ServiceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            this.LoadExtensions(arguments.Extensions);

            return this.ParseStandardCommandLine(arguments);
        }

        public ICommandLineCommand CreateCommand(string commandLine)
        {
            var arguments = this.ServiceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(commandLine);

            this.LoadExtensions(arguments.Extensions);

            return this.ParseStandardCommandLine(arguments);
        }

        public ICommandLineCommand ParseStandardCommandLine(ICommandLineArguments arguments)
        {
            var context = this.ServiceProvider.GetService<ICommandLineContext>();
            context.ExtensionManager = this.ServiceProvider.GetService<IExtensionManager>();
            context.Arguments = arguments;

            var command = this.Parse(context);

            if (!this.SuppressLogo && command?.ShowLogo == true)
            {
                var branding = this.ServiceProvider.GetService<IWixBranding>();
                Console.WriteLine(branding.ReplacePlaceholders("[AssemblyProduct] version [ProductVersion]"));
                Console.WriteLine(branding.ReplacePlaceholders("[AssemblyCopyright]"));
                Console.WriteLine();
            }

            return command;
        }

        private void LoadExtensions(string[] extensions)
        {
            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

            foreach (var extension in extensions)
            {
                extensionManager.Load(extension);
            }
        }

        private ICommandLineCommand Parse(ICommandLineContext context)
        {
            var extensions = context.ExtensionManager.GetServices<IExtensionCommandLine>();

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
                // If we don't have a command yet, try to parse for a command or a global switch.
                if (command == null)
                {
                    if (this.TryParseCommand(arg, parser, extensions, out command))
                    {
                        // Found our command, all good.
                    }
                    else if (!parser.IsSwitch(arg) || !TryParseCommandLineArgumentWithExtension(arg, parser, extensions))
                    {
                        // Not a global switch handled by an extension, so failure.
                        parser.ReportErrorArgument(arg);
                    }
                }
                else if (parser.IsSwitch(arg))
                {
                    // Commands get first crack at parsing switches then extensions then the standard.
                    if (!command.TryParseArgument(parser, arg) &&
                        !TryParseCommandLineArgumentWithExtension(arg, parser, extensions) &&
                        !this.TryParseStandardCommandLineSwitch(parser, arg))
                    {
                        parser.ReportErrorArgument(arg);
                    }
                }
                else if (!TryParseCommandLineArgumentWithExtension(arg, parser, extensions) &&
                         !command.TryParseArgument(parser, arg))
                {
                    parser.ReportErrorArgument(arg);
                }
            }

            foreach (var extension in extensions)
            {
                extension.PostParse();
            }

            // If we hit an error, do not return a command.
            if (!String.IsNullOrEmpty(parser.ErrorArgument))
            {
                command = null;
            }
            else if (this.ShowHelp || command == null)
            {
                var branding = context.ServiceProvider.GetService<IWixBranding>();
                command = new HelpCommand(extensions, branding, command);
            }

            return command;
        }

        private bool TryParseCommand(string arg, ICommandLineParser parser, IEnumerable<IExtensionCommandLine> extensions, out ICommandLineCommand command)
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
                        var branding = this.ServiceProvider.GetService<IWixBranding>();
                        command = new HelpCommand(extensions, branding, null);
                        break;

                    case "version":
                    case "-version":
                        command = new VersionCommand();
                        break;
                }
            }
            else
            {
                if ("build".Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    command = new BuildCommand(this.ServiceProvider);
                }
                else
                {
                    foreach (var extension in extensions)
                    {
                        if (extension.TryParseCommand(parser, arg, out command))
                        {
                            break;
                        }
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

        private bool TryParseStandardCommandLineSwitch(ICommandLineParser parser, string arg)
        {
            var parameter = arg.Substring(1).ToLowerInvariant();

            switch (parameter)
            {
                case "?":
                case "h":
                case "help":
                case "-help":
                    this.ShowHelp = true;
                    return true;

                case "nologo":
                case "-nologo":
                    this.SuppressLogo = true;
                    return true;

                case "v":
                case "verbose":
                    this.Messaging.ShowVerboseMessages = true;
                    return true;
            }

            if (parameter.StartsWith("sw"))
            {
                this.ParseSuppressWarning(parameter, "sw".Length, parser);
                return true;
            }
            else if (parameter.StartsWith("suppresswarning"))
            {
                this.ParseSuppressWarning(parameter, "suppresswarning".Length, parser);
                return true;
            }
            else if (parameter.StartsWith("wx"))
            {
                this.ParseWarningAsError(parameter, "wx".Length, parser);
                return true;
            }

            return false;
        }

        private void ParseSuppressWarning(string parameter, int offset, ICommandLineParser parser)
        {
            var paramArg = parameter.Substring(offset);
            if (paramArg.Length == 0)
            {
                this.Messaging.SuppressAllWarnings = true;
            }
            else if (Int32.TryParse(paramArg, out var suppressWarning) && suppressWarning > 0)
            {
                this.Messaging.SuppressWarningMessage(suppressWarning);
            }
            else
            {
                parser.ReportErrorArgument(parameter, ErrorMessages.IllegalSuppressWarningId(paramArg));
            }
        }

        private void ParseWarningAsError(string parameter, int offset, ICommandLineParser parser)
        {
            var paramArg = parameter.Substring(offset);
            if (paramArg.Length == 0)
            {
                this.Messaging.WarningsAsError = true;
            }
            else if (Int32.TryParse(paramArg, out var elevateWarning) && elevateWarning > 0)
            {
                this.Messaging.ElevateWarningMessage(elevateWarning);
            }
            else
            {
                parser.ReportErrorArgument(parameter, ErrorMessages.IllegalWarningIdAsError(paramArg));
            }
        }
    }
}
