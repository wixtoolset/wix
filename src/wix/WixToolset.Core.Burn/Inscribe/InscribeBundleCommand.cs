// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Inscribe
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;

    internal class InscribeBundleCommand
    {
        public InscribeBundleCommand(IServiceProvider serviceProvider, string inputPath, string signedEngineFile, string outputPath, string intermediateFolder)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();
            this.IntermediateFolder = intermediateFolder;
            this.InputFilePath = inputPath;
            this.SignedEngineFile = signedEngineFile;
            this.OutputFile = outputPath;
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private string IntermediateFolder { get; }

        private string InputFilePath { get; }

        private string SignedEngineFile { get; }

        private string OutputFile { get; }

        public bool Execute()
        {
            var inscribed = false;
            var tempFile = Path.Combine(this.IntermediateFolder, "~bundle_engine_signed.exe");

            using (var reader = BurnReader.Open(this.Messaging, this.FileSystem, this.InputFilePath))
            {
                this.FileSystem.CopyFile(null, this.SignedEngineFile, tempFile, allowHardlink: false);

                using (var writer = BurnWriter.Open(this.Messaging, this.FileSystem, tempFile))
                {
                    inscribed = writer.ReattachContainers(reader);
                }
            }

            this.FileSystem.MoveFile(null, tempFile, this.OutputFile);

            return inscribed;
        }
    }
}
