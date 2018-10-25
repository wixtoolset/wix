// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class MstBackend : IBackend
    {
        public BindResult Bind(IBindContext context)
        {
#if REVISIT_FOR_PATCHING
            var command = new BindTransformCommand();
            command.Extensions = context.Extensions;
            command.TempFilesLocation = context.IntermediateFolder;
            command.Transform = context.IntermediateRepresentation;
            command.OutputPath = context.OutputPath;
            command.Execute();

            return new BindResult(Array.Empty<FileTransfer>(), Array.Empty<string>());
#endif
            throw new NotImplementedException();
        }

        public DecompileResult Decompile(IDecompileContext context)
        {
            throw new NotImplementedException();
        }

        public bool Inscribe(IInscribeContext context)
        {
            throw new NotImplementedException();
        }

        public Intermediate Unbind(IUnbindContext context)
        {
            var command = new UnbindMsiOrMsmCommand(context);
            return command.Execute();
        }
    }
}