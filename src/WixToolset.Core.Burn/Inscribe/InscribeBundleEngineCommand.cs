// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Inscribe
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Data;

    internal class InscribeBundleEngineCommand
    {
        public InscribeBundleEngineCommand(IInscribeContext context)
        {
            this.Context = context;
        }

        private IInscribeContext Context { get; }

        public bool Execute()
        {
            string tempFile = Path.Combine(this.Context.IntermediateFolder, "bundle_engine_unsigned.exe");

            using (BurnReader reader = BurnReader.Open(this.Context.InputFilePath))
            using (FileStream writer = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))
            {
                reader.Stream.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[4 * 1024];
                int total = 0;
                int read = 0;
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

            Directory.CreateDirectory(Path.GetDirectoryName(this.Context.OutputFile));
            if (File.Exists(this.Context.OutputFile))
            {
                File.Delete(this.Context.OutputFile);
            }

            File.Move(tempFile, this.Context.OutputFile);
            WixToolset.Core.Native.NativeMethods.ResetAcls(new string[] { this.Context.OutputFile }, 1);

            return true;
        }
    }
}
