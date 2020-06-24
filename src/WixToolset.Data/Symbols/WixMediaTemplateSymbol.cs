// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixMediaTemplate = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixMediaTemplate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.CabinetTemplate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.CompressionLevel), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.VolumeLabel), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.MaximumUncompressedMediaSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateSymbolFields.MaximumCabinetSizeForLargeFileSplitting), IntermediateFieldType.Number),
            },
            typeof(WixMediaTemplateSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixMediaTemplateSymbolFields
    {
        CabinetTemplate,
        CompressionLevel,
        DiskPrompt,
        VolumeLabel,
        MaximumUncompressedMediaSize,
        MaximumCabinetSizeForLargeFileSplitting,
    }

    public class WixMediaTemplateSymbol : IntermediateSymbol
    {
        public WixMediaTemplateSymbol() : base(SymbolDefinitions.WixMediaTemplate, null, null)
        {
        }

        public WixMediaTemplateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixMediaTemplate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMediaTemplateSymbolFields index] => this.Fields[(int)index];

        public string CabinetTemplate
        {
            get => (string)this.Fields[(int)WixMediaTemplateSymbolFields.CabinetTemplate];
            set => this.Set((int)WixMediaTemplateSymbolFields.CabinetTemplate, value);
        }

        public CompressionLevel? CompressionLevel
        {
            get => (CompressionLevel?)this.Fields[(int)WixMediaTemplateSymbolFields.CompressionLevel].AsNullableNumber();
            set => this.Set((int)WixMediaTemplateSymbolFields.CompressionLevel, (int?)value);
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)WixMediaTemplateSymbolFields.DiskPrompt];
            set => this.Set((int)WixMediaTemplateSymbolFields.DiskPrompt, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)WixMediaTemplateSymbolFields.VolumeLabel];
            set => this.Set((int)WixMediaTemplateSymbolFields.VolumeLabel, value);
        }

        public int? MaximumUncompressedMediaSize
        {
            get => (int?)this.Fields[(int)WixMediaTemplateSymbolFields.MaximumUncompressedMediaSize];
            set => this.Set((int)WixMediaTemplateSymbolFields.MaximumUncompressedMediaSize, value);
        }

        public int? MaximumCabinetSizeForLargeFileSplitting
        {
            get => (int?)this.Fields[(int)WixMediaTemplateSymbolFields.MaximumCabinetSizeForLargeFileSplitting];
            set => this.Set((int)WixMediaTemplateSymbolFields.MaximumCabinetSizeForLargeFileSplitting, value);
        }
    }
}