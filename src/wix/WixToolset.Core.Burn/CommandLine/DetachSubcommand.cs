// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Burn.Inscribe;
    using WixToolset.Extensibility.Services;

    internal class DetachSubcommand : BurnSubcommandBase
    {
        public DetachSubcommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private string InputPath { get; set; }

        private string IntermediateFolder { get; set; }

        private string EngineOutputPath { get; set; }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                Console.Error.WriteLine("Path to input bundle is required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.EngineOutputPath))
            {
                Console.Error.WriteLine("Path to output the bundle engine is required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            var command = new InscribeBundleEngineCommand(this.ServiceProvider, this.InputPath, this.EngineOutputPath, this.IntermediateFolder);
            command.Execute();

            return Task.FromResult(this.Messaging.LastErrorNumber);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument))
            {
                var parameter = argument.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "engine":
                        this.EngineOutputPath = parser.GetNextArgumentAsFilePathOrError(argument);
                        return true;
                }
            }
            else if (String.IsNullOrEmpty(this.InputPath))
            {
                this.InputPath = argument;
                return true;
            }

            return false;
        }
    }
}
