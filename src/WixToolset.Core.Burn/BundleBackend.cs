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
    using WixToolset.Extensibility.Services;

    internal class BundleBackend : IBackend
    {
        public IBindResult Bind(IBindContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendExtensions = extensionManager.GetServices<IBurnBackendExtension>();

            foreach (var extension in backendExtensions)
            {
                extension.PreBackendBind(context);
            }

            var command = new BindBundleCommand(context, backendExtensions);
            command.Execute();

            var result = context.ServiceProvider.GetService<IBindResult>();
            result.FileTransfers = command.FileTransfers;
            result.TrackedFiles = command.TrackedFiles;

            foreach (var extension in backendExtensions)
            {
                extension.PostBackendBind(result);
            }
            return result;
        }

        public IDecompileResult Decompile(IDecompileContext context)
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
            var uxExtractPath = Path.Combine(context.ExportBasePath, "UX");
            var acExtractPath = Path.Combine(context.ExportBasePath, "AttachedContainer");

            using (var reader = BurnReader.Open(context.InputFilePath))
            {
                reader.ExtractUXContainer(uxExtractPath, context.IntermediateFolder);
                reader.ExtractAttachedContainer(acExtractPath, context.IntermediateFolder);
            }

            return null;
        }
    }
}
