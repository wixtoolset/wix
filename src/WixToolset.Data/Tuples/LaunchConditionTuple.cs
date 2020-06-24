// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition LaunchCondition = new IntermediateSymbolDefinition(
            SymbolDefinitionType.LaunchCondition,
            new[]
            {
                new IntermediateFieldDefinition(nameof(LaunchConditionSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LaunchConditionSymbolFields.Description), IntermediateFieldType.String),
            },
            typeof(LaunchConditionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum LaunchConditionSymbolFields
    {
        Condition,
        Description,
    }

    public class LaunchConditionSymbol : IntermediateSymbol
    {
        public LaunchConditionSymbol() : base(SymbolDefinitions.LaunchCondition, null, null)
        {
        }

        public LaunchConditionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.LaunchCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[LaunchConditionSymbolFields index] => this.Fields[(int)index];

        public string Condition
        {
            get => (string)this.Fields[(int)LaunchConditionSymbolFields.Condition];
            set => this.Set((int)LaunchConditionSymbolFields.Condition, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)LaunchConditionSymbolFields.Description];
            set => this.Set((int)LaunchConditionSymbolFields.Description, value);
        }
    }
}