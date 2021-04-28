// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Services;

    internal class ConvertCommand : FixupCommandBase
    {
        private const string SettingsFileDefault = "wix.convert.settings.xml";

        public ConvertCommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IMessaging Messaging { get; }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.ShowHelp)
            {
                DisplayHelp();
                return Task.FromResult(-1);
            }

            this.ParseSettings(SettingsFileDefault);

            var converter = new WixConverter(this.Messaging, this.IndentationAmount, this.ErrorsAsWarnings, this.IgnoreErrors);

            var errors = base.Inspect(Inspector, cancellationToken);

            return Task.FromResult(errors);

            int Inspector(string file, bool fix)
            {
                return converter.ConvertFile(file, fix);
            }
        }

        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: wix convert [options] sourceFile [sourceFile ...]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h|--help         Show command line help.");
            Console.WriteLine("  --nologo          Suppress displaying the logo information.");
            Console.WriteLine("  -n|--dry-run      Only display errors, do not update files.");
            Console.WriteLine("  -r|--recurse      Search for matching files in current dir and subdirs.");
            Console.WriteLine("  -set1<file>       Primary settings file.");
            Console.WriteLine("  -set2<file>       Secondary settings file (overrides primary).");
            Console.WriteLine("  -indent:<n>       Indentation multiple (overrides default of 4).");
            Console.WriteLine();
            Console.WriteLine("  sourceFile may use wildcards like *.wxs");
        }
    }
}
