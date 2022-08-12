// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;

    internal class FormatCommand : FixupCommandBase
    {
        private const string SettingsFileDefault = "wix.format.settings.xml";

        public FormatCommand(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Ensures consistent formatting of source code.", "format [options] sourceFile [sourceFile ...]")
            {
                Switches = new[]
                {
                    new CommandLineHelpSwitch("--dry-run", "-n", "Only display errors, do not update files."),
                    new CommandLineHelpSwitch("--recurse", "-r", "Search for matching files in current dir and subdirs."),
                    new CommandLineHelpSwitch("-set1<file>", "Primary settings file."),
                    new CommandLineHelpSwitch("-set2<file>", "Secondary settings file (overrides primary)."),
                    new CommandLineHelpSwitch("-indent:<n>", "Indentation multiple (overrides default of 4)."),
                },
                Notes = "  sourceFile may use wildcards like *.wxs"
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            this.ParseSettings(SettingsFileDefault);

            var converter = new WixConverter(this.Messaging, this.IndentationAmount, this.ErrorsAsWarnings, this.IgnoreErrors);

            var errors = base.Inspect(Inspector, cancellationToken);

            return Task.FromResult(errors);

            int Inspector(string file, bool fix)
            {
                return converter.FormatFile(file, fix);
            }
        }
    }
}
