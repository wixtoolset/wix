// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System.Collections.Generic;
    using WixToolset.Data.Tuples;

    public class FileFacade
    {
        public FileFacade(FileTuple file, WixFileTuple wixFile, WixDeltaPatchFileTuple deltaPatchFile)
        {
            this.File = file;
            this.WixFile = wixFile;
            this.DeltaPatchFile = deltaPatchFile;
        }

        public FileFacade(bool fromModule, FileTuple file, WixFileTuple wixFile)
        {
            this.FromModule = fromModule;
            this.File = file;
            this.WixFile = wixFile;
        }

        public bool FromModule { get; private set; }

        public FileTuple File { get; private set; }

        public WixFileTuple WixFile { get; private set; }

        public WixDeltaPatchFileTuple DeltaPatchFile { get; private set; }

        /// <summary>
        /// Gets the set of MsiAssemblyName rows created for this file.
        /// </summary>
        /// <value>RowCollection of MsiAssemblyName table.</value>
        public List<MsiAssemblyNameTuple> AssemblyNames { get; set; }

        /// <summary>
        /// Gets or sets the MsiFileHash row for this file.
        /// </summary>
        public MsiFileHashTuple Hash { get; set; }
    }
}
