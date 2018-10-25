// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.WixCop.CommandLine
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.WixCop.Interfaces;

    public sealed class WixCopCommandLineParser : IWixCopCommandLineParser
    {
        private bool fixErrors;
        private int indentationAmount;
        private readonly List<string> searchPatterns;
        private readonly IServiceProvider serviceProvider;
        private string settingsFile1;
        private string settingsFile2;
        private bool showHelp;
        private bool showLogo;
        private bool subDirectories;

        public WixCopCommandLineParser(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.indentationAmount = 4;
            this.searchPatterns = new List<string>();
            this.showLogo = true;
        }

        public ICommandLineArguments Arguments { get; set; }

        public ICommandLineCommand ParseWixCopCommandLine()
        {
            this.Parse();

            if (this.showLogo)
            {
                AppCommon.DisplayToolHeader();
                Console.WriteLine();
            }

            if (this.showHelp)
            {
                return new HelpCommand();
            }

            return new ConvertCommand(
                this.serviceProvider,
                this.fixErrors,
                this.indentationAmount,
                this.searchPatterns,
                this.subDirectories,
                this.settingsFile1,
                this.settingsFile2);
        }

        private void Parse()
        {
            this.showHelp = 0 == this.Arguments.Arguments.Length;
            var parser = this.Arguments.Parse();

            while (!this.showHelp &&
                   String.IsNullOrEmpty(parser.ErrorArgument) &&
                   parser.TryGetNextSwitchOrArgument(out var arg))
            {
                if (String.IsNullOrWhiteSpace(arg)) // skip blank arguments.
                {
                    continue;
                }

                if (parser.IsSwitch(arg))
                {
                    if (!this.ParseArgument(parser, arg))
                    {
                        parser.ErrorArgument = arg;
                    }
                }
                else
                {
                    this.searchPatterns.Add(arg);
                }
            }
        }

        private bool ParseArgument(ICommandLineParser parser, string arg)
        {
            var parameter = arg.Substring(1);

            switch (parameter.ToLowerInvariant())
            {
            case "?":
                this.showHelp = true;
                return true;
            case "f":
                this.fixErrors = true;
                return true;
            case "nologo":
                this.showLogo = false;
                return true;
            case "s":
                this.subDirectories = true;
                return true;
            default: // other parameters
                if (parameter.StartsWith("set1", StringComparison.Ordinal))
                {
                    this.settingsFile1 = parameter.Substring(4);
                }
                else if (parameter.StartsWith("set2", StringComparison.Ordinal))
                {
                    this.settingsFile2 = parameter.Substring(4);
                }
                else if (parameter.StartsWith("indent:", StringComparison.Ordinal))
                {
                    try
                    {
                        this.indentationAmount = Convert.ToInt32(parameter.Substring(7));
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid numeric argument.", parameter);
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid argument.", parameter);
                }
                return true;
            }
        }
    }
}
