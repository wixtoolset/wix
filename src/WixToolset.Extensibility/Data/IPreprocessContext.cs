// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public interface IPreprocessContext
    {
        IWixToolsetServiceProvider ServiceProvider { get; }

        IEnumerable<IPreprocessorExtension> Extensions { get; set; }

        IEnumerable<string> IncludeSearchPaths { get; set; }

        /// <summary>
        /// Gets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform Platform { get; set; }

        string SourcePath { get; set; }

        IDictionary<string, string> Variables { get; set; }

        SourceLineNumber CurrentSourceLineNumber { get; set; }

        CancellationToken CancellationToken { get; set; }
    }
}
