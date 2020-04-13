// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ControlEvent = new IntermediateTupleDefinition(
            TupleDefinitionType.ControlEvent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.ControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.Event), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventTupleFields.Ordering), IntermediateFieldType.Number),
            },
            typeof(ControlEventTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ControlEventTupleFields
    {
        DialogRef,
        ControlRef,
        Event,
        Argument,
        Condition,
        Ordering,
    }

    public class ControlEventTuple : IntermediateTuple
    {
        public ControlEventTuple() : base(TupleDefinitions.ControlEvent, null, null)
        {
        }

        public ControlEventTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ControlEvent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlEventTupleFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlEventTupleFields.DialogRef];
            set => this.Set((int)ControlEventTupleFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)ControlEventTupleFields.ControlRef];
            set => this.Set((int)ControlEventTupleFields.ControlRef, value);
        }

        public string Event
        {
            get => (string)this.Fields[(int)ControlEventTupleFields.Event];
            set => this.Set((int)ControlEventTupleFields.Event, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)ControlEventTupleFields.Argument];
            set => this.Set((int)ControlEventTupleFields.Argument, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ControlEventTupleFields.Condition];
            set => this.Set((int)ControlEventTupleFields.Condition, value);
        }

        public int? Ordering
        {
            get => (int?)this.Fields[(int)ControlEventTupleFields.Ordering];
            set => this.Set((int)ControlEventTupleFields.Ordering, value);
        }
    }
}