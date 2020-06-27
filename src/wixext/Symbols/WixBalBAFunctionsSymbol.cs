// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalBAFunctions = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalBAFunctions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalBAFunctionsSymbolFields.PayloadId), IntermediateFieldType.String),
            },
            typeof(WixBalBAFunctionsSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixBalBAFunctionsSymbolFields
    {
        PayloadId,
    }

    public class WixBalBAFunctionsSymbol : IntermediateSymbol
    {
        public WixBalBAFunctionsSymbol() : base(BalSymbolDefinitions.WixBalBAFunctions, null, null)
        {
        }

        public WixBalBAFunctionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalBAFunctions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalBAFunctionsSymbolFields index] => this.Fields[(int)index];

        public string PayloadId
        {
            get => this.Fields[(int)WixBalBAFunctionsSymbolFields.PayloadId].AsString();
            set => this.Set((int)WixBalBAFunctionsSymbolFields.PayloadId, value);
        }
    }
}