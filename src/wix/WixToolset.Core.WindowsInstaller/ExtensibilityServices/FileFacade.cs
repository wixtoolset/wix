// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.ExtensibilityServices
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;

    internal class FileFacade : IFileFacade
    {
        public FileFacade(FileSymbol file)
        {
            this.Identifier = file.Id;
            this.ComponentRef = file.ComponentRef;
            this.DiskId = file.DiskId ?? 1;
            this.FileName = file.Name;
            this.FileSize = file.FileSize;
            this.Language = file.Language;
            this.PatchGroup = file.PatchGroup;
            this.Sequence = file.Sequence;
            this.SourceLineNumber = file.SourceLineNumbers;
            this.SourcePath = file.Source?.Path;
            this.Compressed = (file.Attributes & FileSymbolAttributes.Compressed) == FileSymbolAttributes.Compressed;
            this.Uncompressed = (file.Attributes & FileSymbolAttributes.Uncompressed) == FileSymbolAttributes.Uncompressed;
            this.Version = file.Version;
            this.AssemblyNameSymbols = new List<MsiAssemblyNameSymbol>();
        }

        public FileFacade(FileRow row)
        {
            this.Identifier = new Identifier(AccessModifier.Section, row.File);
            this.ComponentRef = row.Component;
            this.DiskId = row.DiskId;
            this.FileName = row.FileName;
            this.FileSize = row.FileSize;
            this.Language = row.Language;
            this.PatchGroup = null;
            this.Sequence = row.Sequence;
            this.SourceLineNumber = row.SourceLineNumbers;
            this.SourcePath = row.Source;
            this.Compressed = (row.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed) == WindowsInstallerConstants.MsidbFileAttributesCompressed;
            this.Uncompressed = (row.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) == WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
            this.Version = row.Version;
            this.AssemblyNameSymbols = new List<MsiAssemblyNameSymbol>();
        }

        public string Id => this.Identifier.Id;

        public Identifier Identifier { get; }

        public string ComponentRef { get; }

        public int DiskId { get; set; }

        public string FileName { get; }

        public int FileSize { get; set; }

        public string Language { get; set; }

        public int? PatchGroup { get; }

        public int Sequence { get; set; }

        public SourceLineNumber SourceLineNumber { get; }

        public string SourcePath { get; }

        public bool Compressed { get; }

        public bool Uncompressed { get; }

        public string Version { get; set; }

        public MsiFileHashSymbol MsiFileHashSymbol { get; set; }

        public ICollection<MsiAssemblyNameSymbol> AssemblyNameSymbols { get; }
    }
}
