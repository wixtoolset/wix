// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixProperty = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixProperty,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPropertySymbolFields.PropertyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPropertySymbolFields.Admin), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPropertySymbolFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPropertySymbolFields.Secure), IntermediateFieldType.Bool),
            },
            typeof(WixPropertySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPropertySymbolFields
    {
        PropertyRef,
        Admin,
        Hidden,
        Secure,
    }

    public class WixPropertySymbol : IntermediateSymbol
    {
        public WixPropertySymbol() : base(SymbolDefinitions.WixProperty, null, null)
        {
        }

        public WixPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPropertySymbolFields index] => this.Fields[(int)index];

        public string PropertyRef
        {
            get => (string)this.Fields[(int)WixPropertySymbolFields.PropertyRef];
            set => this.Set((int)WixPropertySymbolFields.PropertyRef, value);
        }

        public bool Admin
        {
            get => (bool)this.Fields[(int)WixPropertySymbolFields.Admin];
            set => this.Set((int)WixPropertySymbolFields.Admin, value);
        }

        public bool Hidden
        {
            get => (bool)this.Fields[(int)WixPropertySymbolFields.Hidden];
            set => this.Set((int)WixPropertySymbolFields.Hidden, value);
        }

        public bool Secure
        {
            get => (bool)this.Fields[(int)WixPropertySymbolFields.Secure];
            set => this.Set((int)WixPropertySymbolFields.Secure, value);
        }
    }
}