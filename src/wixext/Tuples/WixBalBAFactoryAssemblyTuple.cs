// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalBAFactoryAssembly = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalBAFactoryAssembly.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalBAFactorySymbolFields.PayloadId), IntermediateFieldType.String),
            },
            typeof(WixBalBAFactoryAssemblySymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixBalBAFactorySymbolFields
    {
        PayloadId,
    }

    public class WixBalBAFactoryAssemblySymbol : IntermediateSymbol
    {
        public WixBalBAFactoryAssemblySymbol() : base(BalSymbolDefinitions.WixBalBAFactoryAssembly, null, null)
        {
        }

        public WixBalBAFactoryAssemblySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalBAFactoryAssembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalBAFactorySymbolFields index] => this.Fields[(int)index];

        public string PayloadId
        {
            get => this.Fields[(int)WixBalBAFactorySymbolFields.PayloadId].AsString();
            set => this.Set((int)WixBalBAFactorySymbolFields.PayloadId, value);
        }
    }
}