// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Inscribe
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;

    internal class InscribeBundleEngineCommand
    {
        public InscribeBundleEngineCommand(IServiceProvider serviceProvider, string inputPath, string outputPath, string intermediateFolder)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();
            this.IntermediateFolder = intermediateFolder;
            this.InputFilePath = inputPath;
            this.OutputFile = outputPath;
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private string IntermediateFolder { get; }

        private string InputFilePath { get; }

        private string OutputFile { get; }

        public void Execute()
        {
            var tempFile = Path.Combine(this.IntermediateFolder, "bundle_engine_unsigned.exe");

            using (var reader = BurnReader.Open(this.Messaging, this.FileSystem, this.InputFilePath))
            using (var writer = this.FileSystem.OpenFile(null, tempFile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))
            {
                reader.Stream.Seek(0, SeekOrigin.Begin);

                var buffer = new byte[8 * 1024];
                var total = 0;
                var read = 0;
                do
                {
                    read = Math.Min(buffer.Length, (int)reader.EngineSize - total);

                    read = reader.Stream.Read(buffer, 0, read);
                    writer.Write(buffer, 0, read);

                    total += read;
                } while (total < reader.EngineSize && 0 < read);

                if (total != reader.EngineSize)
                {
                    throw new InvalidOperationException("Failed to copy engine out of bundle.");
                }

                // TODO: update writer with detached container signatures.
            }

            this.FileSystem.MoveFile(null, tempFile, this.OutputFile);
        }
    }
}
