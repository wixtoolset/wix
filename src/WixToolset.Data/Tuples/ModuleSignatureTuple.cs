// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleSignature = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleSignature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleSignatureSymbolFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSignatureSymbolFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleSignatureSymbolFields.Version), IntermediateFieldType.String),
            },
            typeof(ModuleSignatureSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleSignatureSymbolFields
    {
        ModuleID,
        Language,
        Version,
    }

    public class ModuleSignatureSymbol : IntermediateSymbol
    {
        public ModuleSignatureSymbol() : base(SymbolDefinitions.ModuleSignature, null, null)
        {
        }

        public ModuleSignatureSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleSignature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleSignatureSymbolFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleSignatureSymbolFields.ModuleID];
            set => this.Set((int)ModuleSignatureSymbolFields.ModuleID, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)ModuleSignatureSymbolFields.Language];
            set => this.Set((int)ModuleSignatureSymbolFields.Language, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)ModuleSignatureSymbolFields.Version];
            set => this.Set((int)ModuleSignatureSymbolFields.Version, value);
        }
    }
}