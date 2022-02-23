// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all librarian extensions implement.
    /// </summary>
    public interface ILibrarianExtension
    {
        /// <summary>
        /// Called at the beginning of combining.
        /// </summary>
        /// <param name="context">Librarian context.</param>
        void PreCombine(ILibraryContext context);

        /// <summary>
        /// Resolves a path to a file path on disk.
        /// </summary>
        /// <param name="sourceLineNumber">Source line number for the path to resolve.</param>
        /// <param name="symbolDefinition">Symbol related to the path to resolve.</param>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Optional resolved file result.</returns>
        IResolveFileResult ResolveFile(SourceLineNumber sourceLineNumber, IntermediateSymbolDefinition symbolDefinition, string path);

        /// <summary>
        /// Called at the end of combining.
        /// </summary>
        /// <param name="result">Combined library result.</param>
        void PostCombine(ILibraryResult result);
    }
}
