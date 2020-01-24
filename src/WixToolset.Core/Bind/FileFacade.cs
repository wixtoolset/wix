// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;

    public class FileFacade
    {
        public FileFacade(FileTuple file, AssemblyTuple assembly)
        {
            this.FileTuple = file;
            this.AssemblyTuple = assembly;
        }

        public FileFacade(bool fromModule, FileTuple file)
        {
            this.FromModule = fromModule;
            this.FileTuple = file;
        }

        internal FileFacade(FileRow row)
        {
            this.FromTransform = true;
            this.FileRow = row;
        }

        public bool FromModule { get; }

        public bool FromTransform { get; }

        private FileRow FileRow { get; }

        private FileTuple FileTuple { get; }

        private AssemblyTuple AssemblyTuple { get; }

        public string Id => this.FileRow == null ? this.FileTuple.Id.Id : this.FileRow.File;

        public Identifier Identifier => this.FileRow == null ? this.FileTuple.Id : throw new NotImplementedException();

        public string ComponentRef => this.FileRow == null ? this.FileTuple.ComponentRef : this.FileRow.Component;

        public int DiskId
        {
            get => this.FileRow == null ? this.FileTuple.DiskId ?? 0 : this.FileRow.DiskId;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileTuple.DiskId = value;
                }
                else
                {
                    this.FileRow.DiskId = value;
                }
            }
        }

        public string FileName => this.FileRow == null ? this.FileTuple.Name : this.FileRow.FileName;

        public int FileSize
        {
            get => this.FileRow == null ? this.FileTuple.FileSize : this.FileRow.FileSize;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileTuple.FileSize = value;
                }
                else
                {
                    this.FileRow.FileSize = value;
                }
            }
        }

        public string Language
        {
            get => this.FileRow == null ? this.FileTuple.Language : this.FileRow.Language;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileTuple.Language = value;
                }
                else
                {
                    this.FileRow.Language = value;
                }
            }
        }

        public int? PatchGroup => this.FileRow == null ? this.FileTuple.PatchGroup : null;

        public int Sequence
        {
            get => this.FileRow == null ? this.FileTuple.Sequence : this.FileRow.Sequence;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileTuple.Sequence = value;
                }
                else
                {
                    this.FileRow.Sequence = value;
                }
            }
        }

        public SourceLineNumber SourceLineNumber => this.FileRow == null ? this.FileTuple.SourceLineNumbers : this.FileRow.SourceLineNumbers;

        public string SourcePath => this.FileRow == null ? this.FileTuple.Source.Path : this.FileRow.Source;

        public bool Compressed => this.FileRow == null ? (this.FileTuple.Attributes & FileTupleAttributes.Compressed) == FileTupleAttributes.Compressed : (this.FileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed) == WindowsInstallerConstants.MsidbFileAttributesCompressed;

        public bool Uncompressed => this.FileRow == null ? (this.FileTuple.Attributes & FileTupleAttributes.Uncompressed) == FileTupleAttributes.Uncompressed : (this.FileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) == WindowsInstallerConstants.MsidbFileAttributesNoncompressed;

        public string Version
        {
            get => this.FileRow == null ? this.FileTuple.Version : this.FileRow.Version;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileTuple.Version = value;
                }
                else
                {
                    this.FileRow.Version = value;
                }
            }
        }

        public AssemblyType? AssemblyType => this.FileRow == null ? this.AssemblyTuple?.Type : throw new NotImplementedException();

        public string AssemblyApplicationFileRef => this.FileRow == null ? this.AssemblyTuple?.ApplicationFileRef : throw new NotImplementedException();

        public string AssemblyManifestFileRef => this.FileRow == null ? this.AssemblyTuple?.ManifestFileRef : throw new NotImplementedException();

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
