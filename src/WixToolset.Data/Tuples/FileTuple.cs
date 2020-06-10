// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition File = new IntermediateTupleDefinition(
            TupleDefinitionType.File,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.FileSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Source), IntermediateFieldType.Path),

                new IntermediateFieldDefinition(nameof(FileTupleFields.FontTitle), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.SelfRegCost), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.BindPath), IntermediateFieldType.String),

                new IntermediateFieldDefinition(nameof(FileTupleFields.Sequence), IntermediateFieldType.Number),

                new IntermediateFieldDefinition(nameof(FileTupleFields.PatchGroup), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.PatchAttributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.DeltaPatchHeaderSource), IntermediateFieldType.String),

                new IntermediateFieldDefinition(nameof(FileTupleFields.RetainLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(FileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum FileTupleFields
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
    public enum FileTupleAttributes : int
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

    public class FileTuple : IntermediateTuple
    {
        public FileTuple() : base(TupleDefinitions.File, null, null)
        {
        }

        public FileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.File, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)FileTupleFields.ComponentRef];
            set => this.Set((int)FileTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)FileTupleFields.Name];
            set => this.Set((int)FileTupleFields.Name, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)FileTupleFields.ShortName];
            set => this.Set((int)FileTupleFields.ShortName, value);
        }

        public int FileSize
        {
            get => (int)this.Fields[(int)FileTupleFields.FileSize];
            set => this.Set((int)FileTupleFields.FileSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)FileTupleFields.Version];
            set => this.Set((int)FileTupleFields.Version, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)FileTupleFields.Language];
            set => this.Set((int)FileTupleFields.Language, value);
        }

        public FileTupleAttributes Attributes
        {
            get => (FileTupleAttributes)this.Fields[(int)FileTupleFields.Attributes].AsNumber();
            set => this.Set((int)FileTupleFields.Attributes, (int)value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)FileTupleFields.DirectoryRef];
            set => this.Set((int)FileTupleFields.DirectoryRef, value);
        }

        public int? DiskId
        {
            get => (int?)this.Fields[(int)FileTupleFields.DiskId];
            set => this.Set((int)FileTupleFields.DiskId, value);
        }

        public IntermediateFieldPathValue Source
        {
            get => this.Fields[(int)FileTupleFields.Source].AsPath();
            set => this.Set((int)FileTupleFields.Source, value);
        }

        public string FontTitle
        {
            get => (string)this.Fields[(int)FileTupleFields.FontTitle];
            set => this.Set((int)FileTupleFields.FontTitle, value);
        }

        public int? SelfRegCost
        {
            get => (int?)this.Fields[(int)FileTupleFields.SelfRegCost];
            set => this.Set((int)FileTupleFields.SelfRegCost, value);
        }

        public string BindPath
        {
            get => (string)this.Fields[(int)FileTupleFields.BindPath];
            set => this.Set((int)FileTupleFields.BindPath, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)FileTupleFields.Sequence];
            set => this.Set((int)FileTupleFields.Sequence, value);
        }

        public int? PatchGroup
        {
            get => (int?)this.Fields[(int)FileTupleFields.PatchGroup];
            set => this.Set((int)FileTupleFields.PatchGroup, value);
        }

        public PatchAttributeType? PatchAttributes
        {
            get => (PatchAttributeType?)this.Fields[(int)FileTupleFields.PatchAttributes].AsNullableNumber();
            set => this.Set((int)FileTupleFields.PatchAttributes, (int?)value);
        }

        public string DeltaPatchHeaderSource
        {
            get => (string)this.Fields[(int)FileTupleFields.DeltaPatchHeaderSource];
            set => this.Set((int)FileTupleFields.DeltaPatchHeaderSource, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)FileTupleFields.RetainLengths];
            set => this.Set((int)FileTupleFields.RetainLengths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)FileTupleFields.IgnoreOffsets];
            set => this.Set((int)FileTupleFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)FileTupleFields.IgnoreLengths];
            set => this.Set((int)FileTupleFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)FileTupleFields.RetainOffsets];
            set => this.Set((int)FileTupleFields.RetainOffsets, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)FileTupleFields.SymbolPaths];
            set => this.Set((int)FileTupleFields.SymbolPaths, value);
        }
    }
}
