// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ExtractSubcommand : BurnSubcommandBase
    {
        public ExtractSubcommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();

            var extensionManager = serviceProvider.GetService<IExtensionManager>();
            this.ContainerExtensions = extensionManager.GetServices<IBurnContainerExtension>();
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private IEnumerable<IBurnContainerExtension> ContainerExtensions { get; }

        private string InputPath { get; set; }

        private string IntermediateFolder { get; set; }

        private string ExtractBootstrapperApplicationPath { get; set; }

        private string ExtractContainersPath { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Extracts the contents of a bundle.", "burn extract [options] bundle.exe -o outputfolder", new[]
            {
                new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified a subfolder will be created under %TMP% folder."),
                new CommandLineHelpSwitch("-outba", "-oba", "Folder to extract the bundle bootstrapper application to."),
                new CommandLineHelpSwitch("-out", "-o", "Folder to extract the bundle contents to."),
            });
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.InputPath))
            {
                this.Messaging.Write(ErrorMessages.FilePathRequired("input bundle"));
            }
            else if (String.IsNullOrEmpty(this.ExtractBootstrapperApplicationPath) && String.IsNullOrEmpty(this.ExtractContainersPath))
            {
                this.Messaging.Write(ErrorMessages.FilePathRequired("output the extracted bundle"));
            }
            else
            {
                if (String.IsNullOrEmpty(this.IntermediateFolder))
                {
                    this.IntermediateFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(this.IntermediateFolder);
                }

                using (var reader = BurnReader.Open(this.Messaging, this.FileSystem, this.InputPath))
                {
                    if (!String.IsNullOrEmpty(this.ExtractBootstrapperApplicationPath))
                    {
                        reader.ExtractUXContainer(this.ExtractBootstrapperApplicationPath, this.IntermediateFolder);
                    }

                    if (!String.IsNullOrEmpty(this.ExtractContainersPath))
                    {
                        try
                        {
                            reader.ExtractAttachedContainers(this.ExtractContainersPath, this.IntermediateFolder, this.ExtractBootstrapperApplicationPath, this.ContainerExtensions);
                        }
                        catch (Exception ex)
                        {
                            this.Messaging.Write(BurnBackendWarnings.FailedToExtractAttachedContainers(null, ex.Message));
                        }

                        try
                        {
                            reader.ExtractDetachedContainers(this.ExtractContainersPath, this.IntermediateFolder, this.ExtractBootstrapperApplicationPath, this.ContainerExtensions);
                        }
                        catch (Exception ex)
                        {
                            this.Messaging.Write(BurnBackendWarnings.FailedToExtractDetachedContainers(null, ex.Message));
                        }
                    }
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

                    case "oba":
                    case "outba":
                        this.ExtractBootstrapperApplicationPath = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "o":
                    case "out":
                        this.ExtractContainersPath = parser.GetNextArgumentAsDirectoryOrError(argument);
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
