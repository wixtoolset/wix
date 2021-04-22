// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Bind context.
    /// </summary>
    public interface IBindContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Counnt of threads to use in cabbing.
        /// </summary>
        int CabbingThreadCount { get; set; }

        /// <summary>
        /// Cabinet cache path.
        /// </summary>
        string CabCachePath { get; set; }

        /// <summary>
        /// Default compression level.
        /// </summary>
        CompressionLevel? DefaultCompressionLevel { get; set; }

        /// <summary>
        /// Delayed fields that need to be resolved again.
        /// </summary>
        IReadOnlyCollection<IDelayedField> DelayedFields { get; set; }

        /// <summary>
        /// Embedded files to extract.
        /// </summary>
        IReadOnlyCollection<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        /// <summary>
        /// Binder extensions.
        /// </summary>
        IReadOnlyCollection<IBinderExtension> Extensions { get; set; }

        /// <summary>
        /// File system extensions.
        /// </summary>
        IReadOnlyCollection<IFileSystemExtension> FileSystemExtensions { get; set; }

        /// <summary>
        /// Set of ICEs to execute.
        /// </summary>
        IReadOnlyCollection<string> Ices { get; set; }

        /// <summary>
        /// Intermedaite folder.
        /// </summary>
        string IntermediateFolder { get; set; }

        /// <summary>
        /// Intermediate representation to bind.
        /// </summary>
        Intermediate IntermediateRepresentation { get; set; }

        /// <summary>
        /// Output path to bind to.
        /// </summary>
        string OutputPath { get; set; }

        /// <summary>
        /// Type of PDB to create.
        /// </summary>
        PdbType PdbType { get; set; }

        /// <summary>
        /// Output path for PDB.
        /// </summary>
        string PdbPath { get; set; }

        /// <summary>
        /// Codepage from resolve.
        /// </summary>
        int? ResolvedCodepage { get; set; }

        /// <summary>
        /// Summary information codepage from resolve.
        /// </summary>
        int? ResolvedSummaryInformationCodepage { get; set; }

        /// <summary>
        /// LCID from resolve.
        /// </summary>
        int? ResolvedLcid { get; set; }

        /// <summary>
        /// Set of ICEs to skip.
        /// </summary>
        IReadOnlyCollection<string> SuppressIces { get; set; }

        /// <summary>
        /// Skip all ICEs.
        /// </summary>
        bool SuppressValidation { get; set; }

        /// <summary>
        /// Skip creation of output.
        /// </summary>
        bool SuppressLayout { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
