// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HarvestPayloads = new IntermediateSymbolDefinition(
            SymbolDefinitionType.HarvestPayloads,
            new[]
            {
                new IntermediateFieldDefinition(nameof(HarvestPayloadsSymbolFields.Inclusions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestPayloadsSymbolFields.Exclusions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestPayloadsSymbolFields.ComplexReferenceParentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestPayloadsSymbolFields.ParentId), IntermediateFieldType.String),
            },
            typeof(HarvestPayloadsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum HarvestPayloadsSymbolFields
    {
        Inclusions,
        Exclusions,
        ComplexReferenceParentType,
        ParentId,
    }

    public class HarvestPayloadsSymbol : IntermediateSymbol
    {
        public HarvestPayloadsSymbol() : base(SymbolDefinitions.HarvestPayloads, null, null)
        {
        }

        public HarvestPayloadsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.HarvestPayloads, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HarvestPayloadsSymbolFields index] => this.Fields[(int)index];

        public string Inclusions
        {
            get => (string)this.Fields[(int)HarvestPayloadsSymbolFields.Inclusions];
            set => this.Set((int)HarvestPayloadsSymbolFields.Inclusions, value);
        }

        public string Exclusions
        {
            get => (string)this.Fields[(int)HarvestPayloadsSymbolFields.Exclusions];
            set => this.Set((int)HarvestPayloadsSymbolFields.Exclusions, value);
        }

        public string ComplexReferenceParentType
        {
            get => (string)this.Fields[(int)HarvestPayloadsSymbolFields.ComplexReferenceParentType];
            set => this.Set((int)HarvestPayloadsSymbolFields.ComplexReferenceParentType, value);
        }

        public string ParentId
        {
            get => (string)this.Fields[(int)HarvestPayloadsSymbolFields.ParentId];
            set => this.Set((int)HarvestPayloadsSymbolFields.ParentId, value);
        }
    }
}
