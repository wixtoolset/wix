// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition AppSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.AppSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AppSearchSymbolFields.PropertyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppSearchSymbolFields.SignatureRef), IntermediateFieldType.String),
            },
            typeof(AppSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum AppSearchSymbolFields
    {
        PropertyRef,
        SignatureRef,
    }

    public class AppSearchSymbol : IntermediateSymbol
    {
        public AppSearchSymbol() : base(SymbolDefinitions.AppSearch, null, null)
        {
        }

        public AppSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.AppSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AppSearchSymbolFields index] => this.Fields[(int)index];

        public string PropertyRef
        {
            get => (string)this.Fields[(int)AppSearchSymbolFields.PropertyRef];
            set => this.Set((int)AppSearchSymbolFields.PropertyRef, value);
        }

        public string SignatureRef
        {
            get => (string)this.Fields[(int)AppSearchSymbolFields.SignatureRef];
            set => this.Set((int)AppSearchSymbolFields.SignatureRef, value);
        }
    }
}