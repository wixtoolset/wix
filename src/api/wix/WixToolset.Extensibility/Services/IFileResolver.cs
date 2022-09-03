// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface to resolve file paths using extensions and bind paths.
    /// </summary>
    public interface IFileResolver
    {
        /// <summary>
        /// Resolves the source path of a file using binder extensions.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="librarianExtensions">Extensions used to resolve the file path.</param>
        /// <param name="bindPaths">Collection of bind paths for the binding stage.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="symbolDefinition">Optional type of source file being resolved.</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        string ResolveFile(string source, IEnumerable<ILibrarianExtension> librarianExtensions, IEnumerable<IBindPath> bindPaths, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition);

        /// <summary>
        /// Resolves the source path of a file using binder extensions.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="resolverExtensions">Extensions used to resolve the file path.</param>
        /// <param name="bindPaths">Collection of bind paths for the binding stage.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="symbolDefinition">Optional type of source file being resolved.</param>
        /// <param name="alreadyCheckedPaths">Optional collection of paths already checked.</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        string ResolveFile(string source, IEnumerable<IResolverExtension> resolverExtensions, IEnumerable<IBindPath> bindPaths, BindStage bindStage, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, IEnumerable<string> alreadyCheckedPaths = null);
    }
}
