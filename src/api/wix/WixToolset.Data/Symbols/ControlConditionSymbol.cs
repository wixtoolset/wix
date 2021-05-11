// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ControlCondition = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ControlCondition,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlConditionSymbolFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionSymbolFields.ControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionSymbolFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionSymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(ControlConditionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ControlConditionSymbolFields
    {
        DialogRef,
        ControlRef,
        Action,
        Condition,
    }

    public class ControlConditionSymbol : IntermediateSymbol
    {
        public ControlConditionSymbol() : base(SymbolDefinitions.ControlCondition, null, null)
        {
        }

        public ControlConditionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ControlCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlConditionSymbolFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlConditionSymbolFields.DialogRef];
            set => this.Set((int)ControlConditionSymbolFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)ControlConditionSymbolFields.ControlRef];
            set => this.Set((int)ControlConditionSymbolFields.ControlRef, value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)ControlConditionSymbolFields.Action];
            set => this.Set((int)ControlConditionSymbolFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ControlConditionSymbolFields.Condition];
            set => this.Set((int)ControlConditionSymbolFields.Condition, value);
        }
    }
}