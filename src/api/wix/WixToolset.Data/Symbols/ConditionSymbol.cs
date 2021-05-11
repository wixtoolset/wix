// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Condition = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Condition,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ConditionSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ConditionSymbolFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ConditionSymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(ConditionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ConditionSymbolFields
    {
        FeatureRef,
        Level,
        Condition,
    }

    public class ConditionSymbol : IntermediateSymbol
    {
        public ConditionSymbol() : base(SymbolDefinitions.Condition, null, null)
        {
        }

        public ConditionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Condition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ConditionSymbolFields index] => this.Fields[(int)index];

        public string FeatureRef
        {
            get => (string)this.Fields[(int)ConditionSymbolFields.FeatureRef];
            set => this.Set((int)ConditionSymbolFields.FeatureRef, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)ConditionSymbolFields.Level];
            set => this.Set((int)ConditionSymbolFields.Level, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ConditionSymbolFields.Condition];
            set => this.Set((int)ConditionSymbolFields.Condition, value);
        }
    }
}