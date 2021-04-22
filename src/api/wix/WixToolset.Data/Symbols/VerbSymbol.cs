// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Verb = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Verb,
            new[]
            {
                new IntermediateFieldDefinition(nameof(VerbSymbolFields.ExtensionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbSymbolFields.Verb), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbSymbolFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(VerbSymbolFields.Command), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbSymbolFields.Argument), IntermediateFieldType.String),
            },
            typeof(VerbSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum VerbSymbolFields
    {
        ExtensionRef,
        Verb,
        Sequence,
        Command,
        Argument,
    }

    public class VerbSymbol : IntermediateSymbol
    {
        public VerbSymbol() : base(SymbolDefinitions.Verb, null, null)
        {
        }

        public VerbSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Verb, sourceLineNumber, id)
        {
        }

        public IntermediateField this[VerbSymbolFields index] => this.Fields[(int)index];

        public string ExtensionRef
        {
            get => (string)this.Fields[(int)VerbSymbolFields.ExtensionRef];
            set => this.Set((int)VerbSymbolFields.ExtensionRef, value);
        }

        public string Verb
        {
            get => (string)this.Fields[(int)VerbSymbolFields.Verb];
            set => this.Set((int)VerbSymbolFields.Verb, value);
        }

        public int? Sequence
        {
            get => (int?)this.Fields[(int)VerbSymbolFields.Sequence];
            set => this.Set((int)VerbSymbolFields.Sequence, value);
        }

        public string Command
        {
            get => (string)this.Fields[(int)VerbSymbolFields.Command];
            set => this.Set((int)VerbSymbolFields.Command, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)VerbSymbolFields.Argument];
            set => this.Set((int)VerbSymbolFields.Argument, value);
        }
    }
}