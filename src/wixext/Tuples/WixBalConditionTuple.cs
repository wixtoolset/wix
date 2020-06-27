// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalCondition = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalCondition.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalConditionSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalConditionSymbolFields.Message), IntermediateFieldType.String),
            },
            typeof(WixBalConditionSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixBalConditionSymbolFields
    {
        Condition,
        Message,
    }

    public class WixBalConditionSymbol : IntermediateSymbol
    {
        public WixBalConditionSymbol() : base(BalSymbolDefinitions.WixBalCondition, null, null)
        {
        }

        public WixBalConditionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalConditionSymbolFields index] => this.Fields[(int)index];

        public string Condition
        {
            get => this.Fields[(int)WixBalConditionSymbolFields.Condition].AsString();
            set => this.Set((int)WixBalConditionSymbolFields.Condition, value);
        }

        public string Message
        {
            get => this.Fields[(int)WixBalConditionSymbolFields.Message].AsString();
            set => this.Set((int)WixBalConditionSymbolFields.Message, value);
        }
    }
}