// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Complus = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Complus,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComplusSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComplusSymbolFields.ExpType), IntermediateFieldType.Number),
            },
            typeof(ComplusSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ComplusSymbolFields
    {
        ComponentRef,
        ExpType,
    }

    public class ComplusSymbol : IntermediateSymbol
    {
        public ComplusSymbol() : base(SymbolDefinitions.Complus, null, null)
        {
        }

        public ComplusSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Complus, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComplusSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ComplusSymbolFields.ComponentRef];
            set => this.Set((int)ComplusSymbolFields.ComponentRef, value);
        }

        public int? ExpType
        {
            get => (int?)this.Fields[(int)ComplusSymbolFields.ExpType];
            set => this.Set((int)ComplusSymbolFields.ExpType, value);
        }
    }
}