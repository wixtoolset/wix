// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Preprocessor context.
    /// </summary>
    public interface IPreprocessContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Collection of extensions to use during preprocessing.
        /// </summary>
        IReadOnlyCollection<IPreprocessorExtension> Extensions { get; set; }

        /// <summary>
        /// Collection of search paths to find include files.
        /// </summary>
        IReadOnlyCollection<string> IncludeSearchPaths { get; set; }

        /// <summary>
        /// Gets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform Platform { get; set; }

        /// <summary>
        /// Path to the source file being preprocessed.
        /// </summary>
        string SourcePath { get; set; }

        /// <summary>
        /// Collection of name/value pairs used as preprocessor variables.
        /// </summary>
        IDictionary<string, string> Variables { get; set; }

        /// <summary>
        /// Current source line number of the preprocessor.
        /// </summary>
        SourceLineNumber CurrentSourceLineNumber { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
