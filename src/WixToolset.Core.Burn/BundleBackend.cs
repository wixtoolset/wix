// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Burn.Inscribe;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class BundleBackend : IBackend
    {
        public BindResult Bind(IBindContext context)
        {
            BindBundleCommand command = new BindBundleCommand(context);
            //command.DefaultCompressionLevel = context.DefaultCompressionLevel;
            //command.Extensions = context.Extensions;
            //command.IntermediateFolder = context.IntermediateFolder;
            //command.Output = context.IntermediateRepresentation;
            //command.OutputPath = context.OutputPath;
            //command.PdbFile = context.OutputPdbPath;
            //command.WixVariableResolver = context.WixVariableResolver;
            command.Execute();

            return new BindResult { FileTransfers = command.FileTransfers, TrackedFiles = command.TrackedFiles };
        }

        public DecompileResult Decompile(IDecompileContext context)
        {
            throw new NotImplementedException();
        }

        public bool Inscribe(IInscribeContext context)
        {
            if (String.IsNullOrEmpty(context.SignedEngineFile))
            {
                var command = new InscribeBundleCommand(context);
                return command.Execute();
            }
            else
            {
                var command = new InscribeBundleEngineCommand(context);
                return command.Execute();
            }
        }

        public Intermediate Unbind(IUnbindContext context)
        {
            string uxExtractPath = Path.Combine(context.ExportBasePath, "UX");
            string acExtractPath = Path.Combine(context.ExportBasePath, "AttachedContainer");

            using (BurnReader reader = BurnReader.Open(context.InputFilePath))
            {
                reader.ExtractUXContainer(uxExtractPath, context.IntermediateFolder);
                reader.ExtractAttachedContainer(acExtractPath, context.IntermediateFolder);
            }

            return null;
        }
    }
}
