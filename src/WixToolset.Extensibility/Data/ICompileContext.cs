// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Context provided to the compiler.
    /// </summary>
    public interface ICompileContext
    {
        /// <summary>
        /// Service provider made available to the compiler and its extensions.
        /// </summary>
        IWixToolsetServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Unique identifier for the compilation.
        /// </summary>
        string CompilationId { get; set; }

        /// <summary>
        /// Set of extensions provided to the compiler.
        /// </summary>
        IEnumerable<ICompilerExtension> Extensions { get; set; }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform Platform { get; set; }

        /// <summary>
        /// Calculates whether the target platform for the compilation is 64-bit or not.
        /// </summary>
        bool IsCurrentPlatform64Bit { get; }

        /// <summary>
        /// Source document being compiled.
        /// </summary>
        XDocument Source { get; set; }

        /// <summary>
        /// Cancellation token to abort cancellation.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
