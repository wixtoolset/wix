// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class MsmBackend : IBackend
    {
        public BindResult Bind(IBindContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendExtensions = extensionManager.Create<IWindowsInstallerBackendBinderExtension>();

            foreach (var extension in backendExtensions)
            {
                extension.PreBackendBind(context);
            }

            var validator = Validator.CreateFromContext(context, "mergemod.cub");

            var command = new BindDatabaseCommand(context, backendExtensions, validator);
            command.Execute();

            var result = new BindResult { FileTransfers = command.FileTransfers, TrackedFiles = command.TrackedFiles };

            foreach (var extension in backendExtensions)
            {
                extension.PostBackendBind(result, command.Pdb);
            }

            if (!String.IsNullOrEmpty(context.OutputPdbPath))
            {
                command.Pdb?.Save(context.OutputPdbPath);
            }

            return result;
        }

        public DecompileResult Decompile(IDecompileContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendExtensions = extensionManager.Create<IWindowsInstallerBackendDecompilerExtension>();

            foreach (var extension in backendExtensions)
            {
                extension.PreBackendDecompile(context);
            }

            var command = new DecompileMsiOrMsmCommand(context, backendExtensions);
            var result = command.Execute();

            foreach (var extension in backendExtensions)
            {
                extension.PostBackendDecompile(result);
            }

            return result;
        }

        public bool Inscribe(IInscribeContext context)
        {
            return false;
        }

        public Intermediate Unbind(IUnbindContext context)
        {
            var command = new UnbindMsiOrMsmCommand(context);
            return command.Execute();
        }
    }
}
