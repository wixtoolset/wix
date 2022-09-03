// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Interface that provides a common facade over file information.
    /// </summary>
    public interface IFileFacade
    {
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
        /// Calculated hash of the file.
        /// </summary>
        MsiFileHashSymbol MsiFileHashSymbol { get; set; }

        /// <summary>
        /// Assembly names found in the file.
        /// </summary>
        ICollection<MsiAssemblyNameSymbol> AssemblyNameSymbols { get; }
    }
}
