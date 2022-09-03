// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// A cabinet builder work item.
    /// </summary>
    internal sealed class CabinetWorkItem
    {
        /// <summary>
        /// Instantiate a new CabinetWorkItem.
        /// </summary>
        /// <param name="sourceLineNumber">Source line number that requires the cabinet creation.</param>
        /// <param name="diskId"></param>
        /// <param name="fileFacades">The collection of files in this cabinet.</param>
        /// <param name="hashesByFileId">The hashes for unversioned files.</param>
        /// <param name="cabinetFile">The cabinet file.</param>
        /// <param name="maxThreshold">Maximum threshold for each cabinet.</param>
        /// <param name="compressionLevel">The compression level of the cabinet.</param>
        /// <param name="modularizationSuffix">Modularization suffix used when building a Merge Module.</param>
        public CabinetWorkItem(SourceLineNumber sourceLineNumber, int diskId, string cabinetFile, IEnumerable<IFileFacade> fileFacades, Dictionary<string, MsiFileHashSymbol> hashesByFileId, int maxThreshold, CompressionLevel compressionLevel, string modularizationSuffix)
        {
            this.SourceLineNumber = sourceLineNumber;
            this.DiskId = diskId;
            this.CabinetFile = cabinetFile;
            this.CompressionLevel = compressionLevel;
            this.ModularizationSuffix = modularizationSuffix;
            this.FileFacades = fileFacades;
            this.HashesByFileId = hashesByFileId;
            this.MaxThreshold = maxThreshold;
        }

        /// <summary>
        /// Source line that requires the cabinet creation.
        /// </summary>
        public SourceLineNumber SourceLineNumber { get; }

        /// <summary>
        /// Gets the Media symbol's DiskId that requires the cabinet.
        /// </summary>
        public int DiskId { get; }

        /// <summary>
        /// Gets the cabinet file.
        /// </summary>
        /// <value>The cabinet file.</value>
        public string CabinetFile { get; }

        /// <summary>
        /// Gets the compression level of the cabinet.
        /// </summary>
        /// <value>The compression level of the cabinet.</value>
        public CompressionLevel CompressionLevel { get; }

        /// <summary>
        /// Gets the modularization suffix used when building a Merge Module.
        /// </summary>
        public string ModularizationSuffix { get; }

        /// <summary>
        /// Gets the collection of files in this cabinet.
        /// </summary>
        /// <value>The collection of files in this cabinet.</value>
        public IEnumerable<IFileFacade> FileFacades { get; }

        /// <summary>
        /// The hashes for unversioned files.
        /// </summary>
        public Dictionary<string, MsiFileHashSymbol> HashesByFileId { get; }

        /// <summary>
        /// Gets the max threshold.
        /// </summary>
        /// <value>The maximum threshold for a folder in a cabinet.</value>
        public int MaxThreshold { get; }
    }
}
