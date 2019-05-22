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
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingTupleFields.ControlRef), IntermediateFieldType.String),
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
        DialogRef,
        ControlRef,
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

        public string DialogRef
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.DialogRef];
            set => this.Set((int)EventMappingTupleFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)EventMappingTupleFields.ControlRef];
            set => this.Set((int)EventMappingTupleFields.ControlRef, value);
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