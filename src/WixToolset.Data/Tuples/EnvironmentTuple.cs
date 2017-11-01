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
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Environment), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EnvironmentTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(EnvironmentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum EnvironmentTupleFields
    {
        Environment,
        Name,
        Value,
        Component_,
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

        public string Environment
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Environment]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Environment, value);
        }

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

        public string Component_
        {
            get => (string)this.Fields[(int)EnvironmentTupleFields.Component_]?.Value;
            set => this.Set((int)EnvironmentTupleFields.Component_, value);
        }
    }
}