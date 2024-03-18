// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BundleBackend : IBackend
    {
        public IBindResult Bind(IBindContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendExtensions = extensionManager.GetServices<IBurnBackendBinderExtension>();
            var containerExtensions = extensionManager.GetServices<IBurnContainerExtension>();

            foreach (var extension in backendExtensions)
            {
                extension.PreBackendBind(context);
            }

            foreach (var extension in containerExtensions)
            {
                extension.PreBackendBind(context);
            }

            var command = new BindBundleCommand(context, backendExtensions, containerExtensions);
            command.Execute();

            var result = context.ServiceProvider.GetService<IBindResult>();
            result.FileTransfers = command.FileTransfers;
            result.TrackedFiles = command.TrackedFiles;
            result.Wixout = command.Wixout;

            foreach (var extension in backendExtensions)
            {
                extension.PostBackendBind(result);
            }

            return result;
        }
    }
}
