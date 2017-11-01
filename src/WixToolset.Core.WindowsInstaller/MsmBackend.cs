// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class MsmBackend : IBackend
    {
        public BindResult Bind(IBindContext context)
        {
            var validator = Validator.CreateFromContext(context, "mergemod.cub");

            var command = new BindDatabaseCommand(context, validator);
            command.Execute();

            return new BindResult(command.FileTransfers, command.ContentFilePaths);
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
