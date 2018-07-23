// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    public interface IBackendFactory
    {
        bool TryCreateBackend(string outputType, string outputPath, IBindContext context, out IBackend backend);
    }
}
