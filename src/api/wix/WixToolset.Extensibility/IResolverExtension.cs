// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all resolver extensions implement.
    /// </summary>
    public interface IResolverExtension
    {
        /// <summary>
        /// Called before resolving occurs.
        /// </summary>
        void PreResolve(IResolveContext context);

        /// <summary>
        /// Called to attempt to resolve source to a file.
        /// </summary>
        IResolveFileResult ResolveFile(string source, IntermediateSymbolDefinition symbolDefinition, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        /// <summary>
        /// Called after all resolving occurs.
        /// </summary>
        void PostResolve(IResolveResult result);
    }
}
