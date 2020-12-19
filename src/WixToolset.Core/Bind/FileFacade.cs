// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;

#pragma warning disable 1591 // TODO: this shouldn't be public, need interface in Extensibility
    public class FileFacade
    {
        public FileFacade(FileSymbol file, AssemblySymbol assembly)
        {
            this.FileSymbol = file;
            this.AssemblySymbol = assembly;

            this.Identifier = file.Id;
            this.ComponentRef = file.ComponentRef;
        }

        public FileFacade(bool fromModule, FileSymbol file)
        {
            this.FromModule = fromModule;
            this.FileSymbol = file;

            this.Identifier = file.Id;
            this.ComponentRef = file.ComponentRef;
        }

        public FileFacade(FileRow row)
        {
            this.FromTransform = true;
            this.FileRow = row;

            this.Identifier = new Identifier(AccessModifier.Private, row.File);
            this.ComponentRef = row.Component;
        }

        public bool FromModule { get; }

        public bool FromTransform { get; }

        private FileRow FileRow { get; }

        private FileSymbol FileSymbol { get; }

        private AssemblySymbol AssemblySymbol { get; }

        public string Id => this.Identifier.Id;

        public Identifier Identifier { get; }

        public string ComponentRef { get; }

        public int DiskId
        {
            get => this.FileRow == null ? this.FileSymbol.DiskId ?? 1 : this.FileRow.DiskId;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileSymbol.DiskId = value;
                }
                else
                {
                    this.FileRow.DiskId = value;
                }
            }
        }

        public string FileName => this.FileRow == null ? this.FileSymbol.Name : this.FileRow.FileName;

        public int FileSize
        {
            get => this.FileRow == null ? this.FileSymbol.FileSize : this.FileRow.FileSize;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileSymbol.FileSize = value;
                }
                else
                {
                    this.FileRow.FileSize = value;
                }
            }
        }

        public string Language
        {
            get => this.FileRow == null ? this.FileSymbol.Language : this.FileRow.Language;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileSymbol.Language = value;
                }
                else
                {
                    this.FileRow.Language = value;
                }
            }
        }

        public int? PatchGroup => this.FileRow == null ? this.FileSymbol.PatchGroup : null;

        public int Sequence
        {
            get => this.FileRow == null ? this.FileSymbol.Sequence : this.FileRow.Sequence;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileSymbol.Sequence = value;
                }
                else
                {
                    this.FileRow.Sequence = value;
                }
            }
        }

        public SourceLineNumber SourceLineNumber => this.FileRow == null ? this.FileSymbol.SourceLineNumbers : this.FileRow.SourceLineNumbers;

        public string SourcePath => this.FileRow == null ? this.FileSymbol.Source.Path : this.FileRow.Source;

        public bool Compressed => this.FileRow == null ? (this.FileSymbol.Attributes & FileSymbolAttributes.Compressed) == FileSymbolAttributes.Compressed : (this.FileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed) == WindowsInstallerConstants.MsidbFileAttributesCompressed;

        public bool Uncompressed => this.FileRow == null ? (this.FileSymbol.Attributes & FileSymbolAttributes.Uncompressed) == FileSymbolAttributes.Uncompressed : (this.FileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) == WindowsInstallerConstants.MsidbFileAttributesNoncompressed;

        public string Version
        {
            get => this.FileRow == null ? this.FileSymbol.Version : this.FileRow.Version;
            set
            {
                if (this.FileRow == null)
                {
                    this.FileSymbol.Version = value;
                }
                else
                {
                    this.FileRow.Version = value;
                }
            }
        }

        public AssemblyType? AssemblyType => this.FileRow == null ? this.AssemblySymbol?.Type : null;

        public string AssemblyApplicationFileRef => this.FileRow == null ? this.AssemblySymbol?.ApplicationFileRef : throw new NotImplementedException();

        public string AssemblyManifestFileRef => this.FileRow == null ? this.AssemblySymbol?.ManifestFileRef : throw new NotImplementedException();

        /// <summary>
        /// Gets the set of MsiAssemblyName rows created for this file.
        /// </summary>
        /// <value>RowCollection of MsiAssemblyName table.</value>
        public List<MsiAssemblyNameSymbol> AssemblyNames { get; set; }

        /// <summary>
        /// Gets or sets the MsiFileHash row for this file.
        /// </summary>
        public MsiFileHashSymbol Hash { get; set; }

        /// <summary>
        /// Allows direct access to the underlying FileRow as requried for patching.
        /// </summary>
        public FileRow GetFileRow() => this.FileRow ?? throw new NotImplementedException();
    }
}
