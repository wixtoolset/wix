// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Component = new IntermediateTupleDefinition(
            TupleDefinitionType.Component,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Component), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.KeyPath), IntermediateFieldType.String),
            },
            typeof(ComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ComponentTupleFields
    {
        Component,
        ComponentId,
        Directory_,
        Attributes,
        Condition,
        KeyPath,
    }

    public class ComponentTuple : IntermediateTuple
    {
        public ComponentTuple() : base(TupleDefinitions.Component, null, null)
        {
        }

        public ComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Component, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComponentTupleFields index] => this.Fields[(int)index];

        public string Component
        {
            get => (string)this.Fields[(int)ComponentTupleFields.Component]?.Value;
            set => this.Set((int)ComponentTupleFields.Component, value);
        }

        public string ComponentId
        {
            get => (string)this.Fields[(int)ComponentTupleFields.ComponentId]?.Value;
            set => this.Set((int)ComponentTupleFields.ComponentId, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)ComponentTupleFields.Directory_]?.Value;
            set => this.Set((int)ComponentTupleFields.Directory_, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)ComponentTupleFields.Attributes]?.Value;
            set => this.Set((int)ComponentTupleFields.Attributes, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ComponentTupleFields.Condition]?.Value;
            set => this.Set((int)ComponentTupleFields.Condition, value);
        }

        public string KeyPath
        {
            get => (string)this.Fields[(int)ComponentTupleFields.KeyPath]?.Value;
            set => this.Set((int)ComponentTupleFields.KeyPath, value);
        }
    }
}