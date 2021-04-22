// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller.Rows;

    /// <summary>
    /// Interface that provides a common facade over <c>FileSymbol</c> and <c>FileRow</c>.
    /// </summary>
    public interface IFileFacade
    {
        /// <summary>
        /// Reference to assembly application for this file.
        /// </summary>
        string AssemblyApplicationFileRef { get; }

        /// <summary>
        /// Reference to assembly manifest for this file.
        /// </summary>
        string AssemblyManifestFileRef { get; }

        /// <summary>
        /// List of assembly name values in the file.
        /// </summary>
        List<MsiAssemblyNameSymbol> AssemblyNames { get; set; }

        /// <summary>
        /// Optionally indicates what sort of assembly the file is.
        /// </summary>
        AssemblyType? AssemblyType { get; }

        /// <summary>
        /// Component containing the file.
        /// </summary>
        string ComponentRef { get; }

        /// <summary>
        /// Indicates whether the file is compressed.
        /// </summary>
        bool Compressed { get; }

        /// <summary>
        /// Disk Id for the file.
        /// </summary>
        int DiskId { get; set; }

        /// <summary>
        /// Name of the file.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Size of the file.
        /// </summary>
        int FileSize { get; set; }

        /// <summary>
        /// Indicates whether the file came from a merge module.
        /// </summary>
        bool FromModule { get; }

        /// <summary>
        /// Indicates whether the file came from a transform.
        /// </summary>
        bool FromTransform { get; }

        /// <summary>
        /// Hash symbol of the file.
        /// </summary>
        MsiFileHashSymbol Hash { get; set; }

        /// <summary>
        /// Underlying identifier of the file.
        /// </summary>
        Identifier Identifier { get; }

        /// <summary>
        /// Helper accessor for the Id of the Identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Language of the file.
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// Optional patch group for the file.
        /// </summary>
        int? PatchGroup { get; }

        /// <summary>
        /// Sequence of the file.
        /// </summary>
        int Sequence { get; set; }

        /// <summary>
        /// Source line number that define the file.
        /// </summary>
        SourceLineNumber SourceLineNumber { get; }

        /// <summary>
        /// Source to the file.
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// Indicates whether the file is to be uncompressed.
        /// </summary>
        bool Uncompressed { get; }

        /// <summary>
        /// Version of the file.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Gets the underlying <c>FileRow</c> if one is present.
        /// </summary>
        /// <returns><c>FileRow</c> if one is present, otherwise throws.</returns>
        FileRow GetFileRow();
    }
}
