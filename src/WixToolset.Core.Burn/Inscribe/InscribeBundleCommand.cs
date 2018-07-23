// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Inscribe
{
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class InscribeBundleCommand
    {
        public InscribeBundleCommand(IInscribeContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }
        
        private IInscribeContext Context { get; }

        public IMessaging Messaging { get; }

        public bool Execute()
        {
            bool inscribed = false;
            string tempFile = Path.Combine(this.Context.IntermediateFolder, "bundle_engine_signed.exe");

            using (BurnReader reader = BurnReader.Open(this.Context.InputFilePath))
            {
                File.Copy(this.Context.SignedEngineFile, tempFile, true);

                // If there was an attached container on the original (unsigned) bundle, put it back.
                if (reader.AttachedContainerSize > 0)
                {
                    reader.Stream.Seek(reader.AttachedContainerAddress, SeekOrigin.Begin);

                    using (BurnWriter writer = BurnWriter.Open(this.Messaging, tempFile))
                    {
                        writer.RememberThenResetSignature();
                        writer.AppendContainer(reader.Stream, reader.AttachedContainerSize, BurnCommon.Container.Attached);
                        inscribed = true;
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(this.Context.OutputFile));
            if (File.Exists(this.Context.OutputFile))
            {
                File.Delete(this.Context.OutputFile);
            }

            File.Move(tempFile, this.Context.OutputFile);
            WixToolset.Core.Native.NativeMethods.ResetAcls(new string[] { this.Context.OutputFile }, 1);

            return inscribed;
        }
    }
}
