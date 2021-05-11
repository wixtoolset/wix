// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of resolving a file.
    /// </summary>
    public interface IResolveFileResult
    {
        /// <summary>
        /// Collection of paths checked to find file.
        /// </summary>
        IReadOnlyCollection<string> CheckedPaths { get; set; }

        /// <summary>
        /// Path to found file, if found.
        /// </summary>
        string Path { get; set; }
    }
}
