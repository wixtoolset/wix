// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a librarian extension.
    /// </summary>
    public abstract class BaseLibrarianExtension : ILibrarianExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected ILibraryContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Called at the beginning of combining.
        /// </summary>
        /// <param name="context">Librarian context.</param>
        public virtual void PreCombine(ILibraryContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        /// <summary>
        /// Resolves a path to a file path on disk.
        /// </summary>
        /// <param name="sourceLineNumber">Source line number for the path to resolve.</param>
        /// <param name="symbolDefinition">Symbol related to the path to resolve.</param>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Optional resolved file result.</returns>
        public virtual IResolveFileResult ResolveFile(SourceLineNumber sourceLineNumber, IntermediateSymbolDefinition symbolDefinition, string path)
        {
            return null;
        }

        /// <summary>
        /// Called at the end of combining.
        /// </summary>
        /// <param name="library">Combined library intermediate.</param>
        public virtual void PostCombine(Intermediate library)
        {
        }

        /// <summary>
        /// Creates an IResolveFileResult.
        /// </summary>
        /// <param name="path">Optional resolved path to file.</param>
        /// <param name="checkedPaths">Optional collection of paths checked for the file.</param>
        /// <returns>Resolved file result.</returns>
        protected IResolveFileResult CreateResolveFileResult(string path = null, IReadOnlyCollection<string> checkedPaths = null)
        {
            var result = this.Context.ServiceProvider.GetService<IResolveFileResult>();
            result.Path = path;
            result.CheckedPaths = checkedPaths;

            return result;
        }
    }
}
