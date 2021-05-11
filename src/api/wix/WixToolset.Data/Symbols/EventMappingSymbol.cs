// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition EventMapping = new IntermediateSymbolDefinition(
            SymbolDefinitionType.EventMapping,
            new[]
            {
                new IntermediateFieldDefinition(nameof(EventMappingSymbolFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingSymbolFields.ControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingSymbolFields.Event), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventMappingSymbolFields.Attribute), IntermediateFieldType.String),
            },
            typeof(EventMappingSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum EventMappingSymbolFields
    {
        DialogRef,
        ControlRef,
        Event,
        Attribute,
    }

    public class EventMappingSymbol : IntermediateSymbol
    {
        public EventMappingSymbol() : base(SymbolDefinitions.EventMapping, null, null)
        {
        }

        public EventMappingSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.EventMapping, sourceLineNumber, id)
        {
        }

        public IntermediateField this[EventMappingSymbolFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)EventMappingSymbolFields.DialogRef];
            set => this.Set((int)EventMappingSymbolFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)EventMappingSymbolFields.ControlRef];
            set => this.Set((int)EventMappingSymbolFields.ControlRef, value);
        }

        public string Event
        {
            get => (string)this.Fields[(int)EventMappingSymbolFields.Event];
            set => this.Set((int)EventMappingSymbolFields.Event, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)EventMappingSymbolFields.Attribute];
            set => this.Set((int)EventMappingSymbolFields.Attribute, value);
        }
    }
}