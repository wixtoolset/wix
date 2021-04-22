// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Context for laying out files.
    /// </summary>
    public interface ILayoutContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Extensions for use during layout.
        /// </summary>
        IReadOnlyCollection<ILayoutExtension> Extensions { get; set; }

        /// <summary>
        /// Set of tracked of files created during processing to be cleaned up.
        /// </summary>
        IReadOnlyCollection<ITrackedFile> TrackedFiles { get; set; }

        /// <summary>
        /// Set of files to transfer.
        /// </summary>
        IReadOnlyCollection<IFileTransfer> FileTransfers { get; set; }

        /// <summary>
        /// File to capture list of content files.
        /// </summary>
        string ContentsFile { get; set; }

        /// <summary>
        /// File to capture list of output files.
        /// </summary>
        string OutputsFile { get; set; }

        /// <summary>
        /// Intermediate folder.
        /// </summary>
        string IntermediateFolder { get; set; }

        /// <summary>
        /// List of built output files.
        /// </summary>
        string BuiltOutputsFile { get; set; }

        /// <summary>
        /// Reset ACLs on file transfers.
        /// </summary>
        bool ResetAcls { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
