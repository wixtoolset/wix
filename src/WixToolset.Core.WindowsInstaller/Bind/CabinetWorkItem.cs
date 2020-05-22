// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using WixToolset.Core.Bind;
    using WixToolset.Data;

    /// <summary>
    /// A cabinet builder work item.
    /// </summary>
    internal sealed class CabinetWorkItem
    {
        /// <summary>
        /// Instantiate a new CabinetWorkItem.
        /// </summary>
        /// <param name="fileFacades">The collection of files in this cabinet.</param>
        /// <param name="cabinetFile">The cabinet file.</param>
        /// <param name="maxThreshold">Maximum threshold for each cabinet.</param>
        /// <param name="compressionLevel">The compression level of the cabinet.</param>
        /// <param name="binderFileManager">The binder file manager.</param>
        public CabinetWorkItem(IEnumerable<FileFacade> fileFacades, string cabinetFile, int maxThreshold, CompressionLevel compressionLevel, string modularizationSuffix /*, BinderFileManager binderFileManager*/)
        {
            this.CabinetFile = cabinetFile;
            this.CompressionLevel = compressionLevel;
            this.ModularizationSuffix = modularizationSuffix;
            this.FileFacades = fileFacades;
            //this.BinderFileManager = binderFileManager;
            this.MaxThreshold = maxThreshold;
        }

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
        public IEnumerable<FileFacade> FileFacades { get;  }

        /// <summary>
        /// Gets the binder file manager.
        /// </summary>
        /// <value>The binder file manager.</value>
        //public BinderFileManager BinderFileManager { get; private set; }

        /// <summary>
        /// Gets the max threshold.
        /// </summary>
        /// <value>The maximum threshold for a folder in a cabinet.</value>
        public int MaxThreshold { get; }
    }
}
