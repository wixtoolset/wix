// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Inscribe
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Native;
    using WixToolset.Extensibility.Data;

    internal class InscribeBundleEngineCommand
    {
        public InscribeBundleEngineCommand(IInscribeContext context)
        {
            this.IntermediateFolder = context.IntermediateFolder;
            this.InputFilePath = context.InputFilePath;
            this.OutputFile = context.OutputFile;
        }

        private string IntermediateFolder { get; }

        private string InputFilePath { get; }

        private string OutputFile { get; }

        public bool Execute()
        {
            var tempFile = Path.Combine(this.IntermediateFolder, "bundle_engine_unsigned.exe");

            using (var reader = BurnReader.Open(this.InputFilePath))
            using (var writer = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))
            {
                reader.Stream.Seek(0, SeekOrigin.Begin);

                var buffer = new byte[4 * 1024];
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

            Directory.CreateDirectory(Path.GetDirectoryName(this.OutputFile));

            FileSystem.MoveFile(tempFile, this.OutputFile);

            return true;
        }
    }
}
