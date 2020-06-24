// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchFamilyGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatchFamilyGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchFamilyGroupSymbolFields.WixPatchFamilyGroup), IntermediateFieldType.String),
            },
            typeof(WixPatchFamilyGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPatchFamilyGroupSymbolFields
    {
        WixPatchFamilyGroup,
    }

    public class WixPatchFamilyGroupSymbol : IntermediateSymbol
    {
        public WixPatchFamilyGroupSymbol() : base(SymbolDefinitions.WixPatchFamilyGroup, null, null)
        {
        }

        public WixPatchFamilyGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchFamilyGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchFamilyGroupSymbolFields index] => this.Fields[(int)index];

        public string WixPatchFamilyGroup
        {
            get => (string)this.Fields[(int)WixPatchFamilyGroupSymbolFields.WixPatchFamilyGroup];
            set => this.Set((int)WixPatchFamilyGroupSymbolFields.WixPatchFamilyGroup, value);
        }
    }
}