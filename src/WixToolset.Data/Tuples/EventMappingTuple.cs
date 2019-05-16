// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition EventMapping = new IntermediateTupleDefinition(
            TupleDefinitionType.EventMapping,
            new[]
            {
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.Dialog_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.Control_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.Event), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.Attribute), IntermediateFieldType.String),
            },
            typeof(EventMappingTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum EventMappingTupleFields
    {
        Dialog_,
        Control_,
        Event,
        Attribute,
    }

    public class EventMappingTuple : IntermediateTuple
    {
        public EventMappingTuple() : base(TupleDefinitions.EventMapping, null, null)
        {
        }

        public EventMappingTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.EventMapping, sourceLineNumber, id)
        {
        }

        public IntermediateField this[EventMappingTupleFields index] => this.Fields[(int)index];

        public string Dialog_
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.Dialog_];
            set => this.Set((int)EventMappingTupleFields.Dialog_, value);
        }

        public string Control_
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.Control_];
            set => this.Set((int)EventMappingTupleFields.Control_, value);
        }

        public string Event
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.Event];
            set => this.Set((int)EventMappingTupleFields.Event, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.Attribute];
            set => this.Set((int)EventMappingTupleFields.Attribute, value);
        }
    }
}