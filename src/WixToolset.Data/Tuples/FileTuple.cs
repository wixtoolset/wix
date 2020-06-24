// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition File = new IntermediateSymbolDefinition(
            SymbolDefinitionType.File,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.FileSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.Source), IntermediateFieldType.Path),

                new IntermediateFieldDefinition(nameof(FileSymbolFields.FontTitle), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.SelfRegCost), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.BindPath), IntermediateFieldType.String),

                new IntermediateFieldDefinition(nameof(FileSymbolFields.Sequence), IntermediateFieldType.Number),

                new IntermediateFieldDefinition(nameof(FileSymbolFields.PatchGroup), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.PatchAttributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.DeltaPatchHeaderSource), IntermediateFieldType.String),

                new IntermediateFieldDefinition(nameof(FileSymbolFields.RetainLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSymbolFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(FileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum FileSymbolFields
    {
        ComponentRef,
        Name,
        ShortName,
        FileSize,
        Version,
        Language,
        Attributes,
        DirectoryRef,
        DiskId,
        Source,

        FontTitle,
        SelfRegCost,
        BindPath,

        Sequence,

        PatchGroup,
        PatchAttributes,
        DeltaPatchHeaderSource,

        RetainLengths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
        SymbolPaths,
    }

    [Flags]
    public enum FileSymbolAttributes : int
    {
        None = 0x0,
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Vital = 0x8,
        Compressed = 0x10,
        Uncompressed = 0x20,
        Checksum = 0x40,
        GeneratedShortFileName = 0x80,
    }

    /// <summary>
    /// PatchAttribute values
    /// </summary>
    [Flags]
    public enum PatchAttributeType
    {
        None = 0,

        /// <summary>Prevents the updating of the file that is in fact changed in the upgraded image relative to the target images.</summary>
        Ignore = 1,

        /// <summary>Set if the entire file should be installed rather than creating a binary patch.</summary>
        IncludeWholeFile = 2,

        /// <summary>Set to indicate that the patch is non-vital.</summary>
        AllowIgnoreOnError = 4,

        /// <summary>Allowed bits.</summary>
        Defined = Ignore | IncludeWholeFile | AllowIgnoreOnError
    }

    public class FileSymbol : IntermediateSymbol
    {
        public FileSymbol() : base(SymbolDefinitions.File, null, null)
        {
        }

        public FileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.File, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)FileSymbolFields.ComponentRef];
            set => this.Set((int)FileSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)FileSymbolFields.Name];
            set => this.Set((int)FileSymbolFields.Name, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)FileSymbolFields.ShortName];
            set => this.Set((int)FileSymbolFields.ShortName, value);
        }

        public int FileSize
        {
            get => (int)this.Fields[(int)FileSymbolFields.FileSize];
            set => this.Set((int)FileSymbolFields.FileSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)FileSymbolFields.Version];
            set => this.Set((int)FileSymbolFields.Version, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)FileSymbolFields.Language];
            set => this.Set((int)FileSymbolFields.Language, value);
        }

        public FileSymbolAttributes Attributes
        {
            get => (FileSymbolAttributes)this.Fields[(int)FileSymbolFields.Attributes].AsNumber();
            set => this.Set((int)FileSymbolFields.Attributes, (int)value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)FileSymbolFields.DirectoryRef];
            set => this.Set((int)FileSymbolFields.DirectoryRef, value);
        }

        public int? DiskId
        {
            get => (int?)this.Fields[(int)FileSymbolFields.DiskId];
            set => this.Set((int)FileSymbolFields.DiskId, value);
        }

        public IntermediateFieldPathValue Source
        {
            get => this.Fields[(int)FileSymbolFields.Source].AsPath();
            set => this.Set((int)FileSymbolFields.Source, value);
        }

        public string FontTitle
        {
            get => (string)this.Fields[(int)FileSymbolFields.FontTitle];
            set => this.Set((int)FileSymbolFields.FontTitle, value);
        }

        public int? SelfRegCost
        {
            get => (int?)this.Fields[(int)FileSymbolFields.SelfRegCost];
            set => this.Set((int)FileSymbolFields.SelfRegCost, value);
        }

        public string BindPath
        {
            get => (string)this.Fields[(int)FileSymbolFields.BindPath];
            set => this.Set((int)FileSymbolFields.BindPath, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)FileSymbolFields.Sequence];
            set => this.Set((int)FileSymbolFields.Sequence, value);
        }

        public int? PatchGroup
        {
            get => (int?)this.Fields[(int)FileSymbolFields.PatchGroup];
            set => this.Set((int)FileSymbolFields.PatchGroup, value);
        }

        public PatchAttributeType? PatchAttributes
        {
            get => (PatchAttributeType?)this.Fields[(int)FileSymbolFields.PatchAttributes].AsNullableNumber();
            set => this.Set((int)FileSymbolFields.PatchAttributes, (int?)value);
        }

        public string DeltaPatchHeaderSource
        {
            get => (string)this.Fields[(int)FileSymbolFields.DeltaPatchHeaderSource];
            set => this.Set((int)FileSymbolFields.DeltaPatchHeaderSource, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)FileSymbolFields.RetainLengths];
            set => this.Set((int)FileSymbolFields.RetainLengths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)FileSymbolFields.IgnoreOffsets];
            set => this.Set((int)FileSymbolFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)FileSymbolFields.IgnoreLengths];
            set => this.Set((int)FileSymbolFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)FileSymbolFields.RetainOffsets];
            set => this.Set((int)FileSymbolFields.RetainOffsets, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)FileSymbolFields.SymbolPaths];
            set => this.Set((int)FileSymbolFields.SymbolPaths, value);
        }
    }
}
