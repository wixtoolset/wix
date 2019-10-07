// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using WixToolset.Extensibility.Data;

    public interface IPathResolver
    {
        /// <summary>
        /// Get the source path of a directory.
        /// </summary>
        /// <param name="directories">All cached directories.</param>
        /// <param name="componentIdGenSeeds">Hash table of Component GUID generation seeds indexed by directory id.</param>
        /// <param name="directory">Directory identifier.</param>
        /// <param name="canonicalize">Canonicalize the path for standard directories.</param>
        /// <returns>Source path of a directory.</returns>
        string GetDirectoryPath(Dictionary<string, IResolvedDirectory> directories, Dictionary<string, string> componentIdGenSeeds, string directory, bool canonicalize);

        /// <summary>
        /// Gets the source path of a file.
        /// </summary>
        /// <param name="directories">All cached directories in <see cref="ResolvedDirectory"/>.</param>
        /// <param name="directoryId">Parent directory identifier.</param>
        /// <param name="fileName">File name (in long|source format).</param>
        /// <param name="compressed">Specifies the package is compressed.</param>
        /// <param name="useLongName">Specifies the package uses long file names.</param>
        /// <returns>Source path of file relative to package directory.</returns>
        string GetFileSourcePath(Dictionary<string, IResolvedDirectory> directories, string directoryId, string fileName, bool compressed, bool useLongName);
    }
}
