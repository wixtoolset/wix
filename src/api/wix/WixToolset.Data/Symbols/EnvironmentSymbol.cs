// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Environment = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Environment,
            new[]
            {
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Separator), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Part), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.Permanent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.System), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(EnvironmentSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(EnvironmentSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum EnvironmentSymbolFields
    {
        Name,
        Value,
        Separator,
        Action,
        Part,
        Permanent,
        System,
        ComponentRef,
    }

    public enum EnvironmentActionType
    {
        Set,
        Create,
        Remove
    }

    public enum EnvironmentPartType
    {
        All,
        First,
        Last
    }

    public class EnvironmentSymbol : IntermediateSymbol
    {
        public EnvironmentSymbol() : base(SymbolDefinitions.Environment, null, null)
        {
        }

        public EnvironmentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Environment, sourceLineNumber, id)
        {
        }

        public IntermediateField this[EnvironmentSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)EnvironmentSymbolFields.Name];
            set => this.Set((int)EnvironmentSymbolFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)EnvironmentSymbolFields.Value];
            set => this.Set((int)EnvironmentSymbolFields.Value, value);
        }

        public string Separator
        {
            get => (string)this.Fields[(int)EnvironmentSymbolFields.Separator];
            set => this.Set((int)EnvironmentSymbolFields.Separator, value);
        }

        public EnvironmentActionType? Action
        {
            get => (EnvironmentActionType?)this.Fields[(int)EnvironmentSymbolFields.Action].AsNullableNumber();
            set => this.Set((int)EnvironmentSymbolFields.Action, (int?)value);
        }

        public EnvironmentPartType? Part
        {
            get => (EnvironmentPartType?)this.Fields[(int)EnvironmentSymbolFields.Part].AsNullableNumber();
            set => this.Set((int)EnvironmentSymbolFields.Part, (int?)value);
        }

        public bool Permanent
        {
            get => this.Fields[(int)EnvironmentSymbolFields.Permanent].AsBool();
            set => this.Set((int)EnvironmentSymbolFields.Permanent, value);
        }

        public bool System
        {
            get => this.Fields[(int)EnvironmentSymbolFields.System].AsBool();
            set => this.Set((int)EnvironmentSymbolFields.System, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)EnvironmentSymbolFields.ComponentRef];
            set => this.Set((int)EnvironmentSymbolFields.ComponentRef, value);
        }
    }
}