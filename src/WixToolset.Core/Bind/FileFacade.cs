// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System.Collections.Generic;
    using WixToolset.Data.Tuples;

    public class FileFacade
    {
        public FileFacade(FileTuple file, AssemblyTuple assembly)
        {
            this.File = file;
            this.Assembly = assembly;
        }

        public FileFacade(bool fromModule, FileTuple file)
        {
            this.FromModule = fromModule;
            this.File = file;
        }

        public bool FromModule { get; }

        public FileTuple File { get; }

        public AssemblyTuple Assembly { get; }

        public int DiskId => this.File.DiskId ?? 0;

        public bool Uncompressed => (this.File.Attributes & FileTupleAttributes.Uncompressed) == FileTupleAttributes.Uncompressed;

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
