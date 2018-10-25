// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    public interface IBackend
    {
        BindResult Bind(IBindContext context);

        DecompileResult Decompile(IDecompileContext context);

        Intermediate Unbind(IUnbindContext context);

        bool Inscribe(IInscribeContext context);
    }
}
