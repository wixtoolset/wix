// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;

    internal class HelpCommand : ICommandLineCommand
    {
        const string HelpMessageOptionFormat = "   {0,-7}  {1}";

        public HelpCommand(IList<IHeatExtension> extensions)
        {
            this.Extensions = extensions;
        }

        private IList<IHeatExtension> Extensions { get; }

        public bool ShowLogo => false;

        public bool StopParsing => true;

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            var exitCode = this.DisplayHelp();
            return Task.FromResult(exitCode);
        }

        public static void DisplayToolHeader()
        {
            var wixcopAssembly = typeof(HelpCommand).Assembly;
            var fv = FileVersionInfo.GetVersionInfo(wixcopAssembly.Location);

            Console.WriteLine("WiX Toolset Harvester version {0}", fv.FileVersion);
            Console.WriteLine("Copyright (C) .NET Foundation and contributors. All rights reserved.");
            Console.WriteLine();
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument) => true;

        private int DisplayHelp()
        {
            DisplayToolHeader();

            // output the harvest types alphabetically
            SortedList harvestOptions = new SortedList();
            foreach (var heatExtension in this.Extensions)
            {
                foreach (HeatCommandLineOption commandLineOption in heatExtension.CommandLineTypes)
                {
                    harvestOptions.Add(commandLineOption.Option, commandLineOption);
                }
            }

            harvestOptions.Add("-nologo", new HeatCommandLineOption("-nologo", "skip printing heat logo information"));
            harvestOptions.Add("-indent <N>", new HeatCommandLineOption("-indent <N>", "indentation multiple (overrides default of 4)"));
            harvestOptions.Add("-o[ut]", new HeatCommandLineOption("-out", "specify output file (default: write to current directory)"));
            harvestOptions.Add("-sw<N>", new HeatCommandLineOption("-sw<N>", "suppress all warnings or a specific message ID\r\n            (example: -sw1011 -sw1012)"));
            harvestOptions.Add("-swall", new HeatCommandLineOption("-swall", "suppress all warnings (deprecated)"));
            harvestOptions.Add("-v", new HeatCommandLineOption("-v", "verbose output"));
            harvestOptions.Add("-wx[N]", new HeatCommandLineOption("-wx[N]", "treat all warnings or a specific message ID as an error\r\n            (example: -wx1011 -wx1012)"));
            harvestOptions.Add("-wxall", new HeatCommandLineOption("-wxall", "treat all warnings as errors (deprecated)"));

            foreach (HeatCommandLineOption commandLineOption in harvestOptions.Values)
            {
                if (!commandLineOption.Option.StartsWith("-"))
                {
                    Console.WriteLine(HelpMessageOptionFormat, commandLineOption.Option, commandLineOption.Description);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Options:");

            foreach (HeatCommandLineOption commandLineOption in harvestOptions.Values)
            {
                if (commandLineOption.Option.StartsWith("-"))
                {
                    Console.WriteLine(HelpMessageOptionFormat, commandLineOption.Option, commandLineOption.Description);
                }
            }

            Console.WriteLine(HelpMessageOptionFormat, "-? | -help", "this help information");
            Console.WriteLine("For more information see: https://wixtoolset.org/");

            return 0;
        }
    }
}
