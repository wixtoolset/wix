// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ExtractSubcommand : BurnSubcommandBase
    {
        public ExtractSubcommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private string InputPath { get; set; }

        private string IntermediateFolder { get; set; }

        private string ExtractPath { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Extracts the internals of a bundle to a folder.", "burn extract [options] bundle.exe -o outputfolder ", new[]
            {
                new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified %TMP% will be used."),
                new CommandLineHelpSwitch("-out", "-o", "Folder to extract the bundle contents to."),
            });
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                Console.Error.WriteLine("Path to input bundle is required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.ExtractPath))
            {
                Console.Error.WriteLine("Path to output the extracted bundle is required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            var uxExtractPath = Path.Combine(this.ExtractPath, "BA");

            using (var reader = BurnReader.Open(this.Messaging, this.FileSystem, this.InputPath))
            {
                reader.ExtractUXContainer(uxExtractPath, this.IntermediateFolder);

                try
                {
                    reader.ExtractAttachedContainers(this.ExtractPath, this.IntermediateFolder);
                }
                catch
                {
                    this.Messaging.Write(BurnBackendWarnings.FailedToExtractAttachedContainers(new Data.SourceLineNumber(this.ExtractPath)));
                }
            }

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

                    case "o":
                    case "out":
                        this.ExtractPath = parser.GetNextArgumentAsDirectoryOrError(argument);
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
