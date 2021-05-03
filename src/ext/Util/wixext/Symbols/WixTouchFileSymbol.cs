// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixTouchFile = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixTouchFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixTouchFileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixTouchFileSymbolFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixTouchFileSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixTouchFileSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixTouchFileSymbolFields
    {
        ComponentRef,
        Path,
        Attributes,
    }

    public class WixTouchFileSymbol : IntermediateSymbol
    {
        public WixTouchFileSymbol() : base(UtilSymbolDefinitions.WixTouchFile, null, null)
        {
        }

        public WixTouchFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixTouchFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixTouchFileSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixTouchFileSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixTouchFileSymbolFields.ComponentRef, value);
        }

        public string Path
        {
            get => this.Fields[(int)WixTouchFileSymbolFields.Path].AsString();
            set => this.Set((int)WixTouchFileSymbolFields.Path, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixTouchFileSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixTouchFileSymbolFields.Attributes, value);
        }
    }
}