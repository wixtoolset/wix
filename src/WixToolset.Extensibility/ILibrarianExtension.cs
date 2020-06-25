// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    public interface ILibrarianExtension
    {
        void PreCombine(ILibraryContext context);

        IResolveFileResult ResolveFile(SourceLineNumber sourceLineNumber, IntermediateSymbolDefinition symbolDefinition, string path);

        void PostCombine(Intermediate library);
    }
}
