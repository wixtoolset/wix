// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiShortcutProperty = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiShortcutProperty,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiShortcutPropertySymbolFields.ShortcutRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiShortcutPropertySymbolFields.PropertyKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiShortcutPropertySymbolFields.PropVariantValue), IntermediateFieldType.String),
            },
            typeof(MsiShortcutPropertySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiShortcutPropertySymbolFields
    {
        ShortcutRef,
        PropertyKey,
        PropVariantValue,
    }

    public class MsiShortcutPropertySymbol : IntermediateSymbol
    {
        public MsiShortcutPropertySymbol() : base(SymbolDefinitions.MsiShortcutProperty, null, null)
        {
        }

        public MsiShortcutPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiShortcutProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiShortcutPropertySymbolFields index] => this.Fields[(int)index];

        public string ShortcutRef
        {
            get => (string)this.Fields[(int)MsiShortcutPropertySymbolFields.ShortcutRef];
            set => this.Set((int)MsiShortcutPropertySymbolFields.ShortcutRef, value);
        }

        public string PropertyKey
        {
            get => (string)this.Fields[(int)MsiShortcutPropertySymbolFields.PropertyKey];
            set => this.Set((int)MsiShortcutPropertySymbolFields.PropertyKey, value);
        }

        public string PropVariantValue
        {
            get => (string)this.Fields[(int)MsiShortcutPropertySymbolFields.PropVariantValue];
            set => this.Set((int)MsiShortcutPropertySymbolFields.PropVariantValue, value);
        }
    }
}