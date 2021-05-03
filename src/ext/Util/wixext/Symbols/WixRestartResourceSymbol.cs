// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixRestartResource = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixRestartResource.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRestartResourceSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRestartResourceSymbolFields.Resource), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRestartResourceSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixRestartResourceSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixRestartResourceSymbolFields
    {
        ComponentRef,
        Resource,
        Attributes,
    }

    public enum WixRestartResourceAttributes
    {
        Filename = 1,
        ProcessName,
        ServiceName,
        TypeMask = 0xf,
    }

    public class WixRestartResourceSymbol : IntermediateSymbol
    {
        public WixRestartResourceSymbol() : base(UtilSymbolDefinitions.WixRestartResource, null, null)
        {
        }

        public WixRestartResourceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixRestartResource, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRestartResourceSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixRestartResourceSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixRestartResourceSymbolFields.ComponentRef, value);
        }

        public string Resource
        {
            get => this.Fields[(int)WixRestartResourceSymbolFields.Resource].AsString();
            set => this.Set((int)WixRestartResourceSymbolFields.Resource, value);
        }

        public WixRestartResourceAttributes? Attributes
        {
            get => (WixRestartResourceAttributes?)this.Fields[(int)WixRestartResourceSymbolFields.Attributes].AsNullableNumber();
            set => this.Set((int)WixRestartResourceSymbolFields.Attributes, (int?)value);
        }
    }
}