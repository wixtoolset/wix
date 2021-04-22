// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixModule = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixModule,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixModuleSymbolFields.ModuleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixModuleSymbolFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixModuleSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixModuleSymbolFields.Codepage), IntermediateFieldType.String),
            },
            typeof(WixModuleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixModuleSymbolFields
    {
        ModuleId,
        Language,
        Version,
        Codepage,
    }

    public class WixModuleSymbol : IntermediateSymbol
    {
        public WixModuleSymbol() : base(SymbolDefinitions.WixModule, null, null)
        {
        }

        public WixModuleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixModule, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixModuleSymbolFields index] => this.Fields[(int)index];

        public string ModuleId
        {
            get => (string)this.Fields[(int)WixModuleSymbolFields.ModuleId];
            set => this.Set((int)WixModuleSymbolFields.ModuleId, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)WixModuleSymbolFields.Language];
            set => this.Set((int)WixModuleSymbolFields.Language, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixModuleSymbolFields.Version];
            set => this.Set((int)WixModuleSymbolFields.Version, value);
        }

        public string Codepage
        {
            get => (string)this.Fields[(int)WixModuleSymbolFields.Codepage];
            set => this.Set((int)WixModuleSymbolFields.Codepage, value);
        }
    }
}
