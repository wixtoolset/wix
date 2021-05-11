// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ControlEvent = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ControlEvent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.ControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.Event), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlEventSymbolFields.Ordering), IntermediateFieldType.Number),
            },
            typeof(ControlEventSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ControlEventSymbolFields
    {
        DialogRef,
        ControlRef,
        Event,
        Argument,
        Condition,
        Ordering,
    }

    public class ControlEventSymbol : IntermediateSymbol
    {
        public ControlEventSymbol() : base(SymbolDefinitions.ControlEvent, null, null)
        {
        }

        public ControlEventSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ControlEvent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlEventSymbolFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlEventSymbolFields.DialogRef];
            set => this.Set((int)ControlEventSymbolFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)ControlEventSymbolFields.ControlRef];
            set => this.Set((int)ControlEventSymbolFields.ControlRef, value);
        }

        public string Event
        {
            get => (string)this.Fields[(int)ControlEventSymbolFields.Event];
            set => this.Set((int)ControlEventSymbolFields.Event, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)ControlEventSymbolFields.Argument];
            set => this.Set((int)ControlEventSymbolFields.Argument, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ControlEventSymbolFields.Condition];
            set => this.Set((int)ControlEventSymbolFields.Condition, value);
        }

        public int? Ordering
        {
            get => (int?)this.Fields[(int)ControlEventSymbolFields.Ordering];
            set => this.Set((int)ControlEventSymbolFields.Ordering, value);
        }
    }
}