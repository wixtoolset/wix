// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Environment = new IntermediateTupleDefinition(
            TupleDefinitionType.Environment,
            new[]
            {
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Separator), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Part), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Permanent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.System), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(EnvironmentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum EnvironmentTupleFields
    {
        Name,
        Value,
        Separator,
        Action,
        Part,
        Permanent,
        System,
        Component_,
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

    public class EnvironmentTuple : IntermediateTuple
    {
        public EnvironmentTuple() : base(TupleDefinitions.Environment, null, null)
        {
        }

        public EnvironmentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Environment, sourceLineNumber, id)
        {
        }

        public IntermediateField this[EnvironmentTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Name]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Value]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Value, value);
        }

        public string Separator
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Separator]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Separator, value);
        }

        public EnvironmentActionType? Action
        {
            get => (EnvironmentActionType?)this.Fields[(int)EnvironmentTupleFields.Action].AsNullableNumber();
            set => this.Set((int)EnvironmentTupleFields.Action, (int)value);
        }

        public EnvironmentPartType? Part
        {
            get => (EnvironmentPartType?)this.Fields[(int)EnvironmentTupleFields.Part].AsNullableNumber();
            set => this.Set((int)EnvironmentTupleFields.Part, (int)value);
        }

        public bool Permanent
        {
            get => this.Fields[(int)EnvironmentTupleFields.Permanent].AsBool();
            set => this.Set((int)EnvironmentTupleFields.Permanent, value);
        }

        public bool System
        {
            get => this.Fields[(int)EnvironmentTupleFields.System].AsBool();
            set => this.Set((int)EnvironmentTupleFields.System, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Component_]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Component_, value);
        }
    }
}