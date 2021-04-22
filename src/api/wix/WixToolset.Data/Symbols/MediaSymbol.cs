// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Media = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Media,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.LastSequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.Cabinet), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.VolumeLabel), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.CompressionLevel), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MediaSymbolFields.Layout), IntermediateFieldType.String),
            },
            typeof(MediaSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MediaSymbolFields
    {
        DiskId,
        LastSequence,
        DiskPrompt,
        Cabinet,
        VolumeLabel,
        Source,
        CompressionLevel,
        Layout,
    }

    public class MediaSymbol : IntermediateSymbol
    {
        public MediaSymbol() : base(SymbolDefinitions.Media, null, null)
        {
        }

        public MediaSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Media, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MediaSymbolFields index] => this.Fields[(int)index];

        public int DiskId
        {
            get => (int)this.Fields[(int)MediaSymbolFields.DiskId];
            set => this.Set((int)MediaSymbolFields.DiskId, value);
        }

        public int? LastSequence
        {
            get => (int?)this.Fields[(int)MediaSymbolFields.LastSequence];
            set => this.Set((int)MediaSymbolFields.LastSequence, value);
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)MediaSymbolFields.DiskPrompt];
            set => this.Set((int)MediaSymbolFields.DiskPrompt, value);
        }

        public string Cabinet
        {
            get => (string)this.Fields[(int)MediaSymbolFields.Cabinet];
            set => this.Set((int)MediaSymbolFields.Cabinet, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)MediaSymbolFields.VolumeLabel];
            set => this.Set((int)MediaSymbolFields.VolumeLabel, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MediaSymbolFields.Source];
            set => this.Set((int)MediaSymbolFields.Source, value);
        }

        public CompressionLevel? CompressionLevel
        {
            get => (CompressionLevel?)this.Fields[(int)MediaSymbolFields.CompressionLevel].AsNullableNumber();
            set => this.Set((int)MediaSymbolFields.CompressionLevel, (int?)value);
        }

        public string Layout
        {
            get => (string)this.Fields[(int)MediaSymbolFields.Layout];
            set => this.Set((int)MediaSymbolFields.Layout, value);
        }
    }
}
