// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Verb = new IntermediateTupleDefinition(
            TupleDefinitionType.Verb,
            new[]
            {
                new IntermediateFieldDefinition(nameof(VerbTupleFields.ExtensionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbTupleFields.Verb), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(VerbTupleFields.Command), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(VerbTupleFields.Argument), IntermediateFieldType.String),
            },
            typeof(VerbTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum VerbTupleFields
    {
        ExtensionRef,
        Verb,
        Sequence,
        Command,
        Argument,
    }

    public class VerbTuple : IntermediateTuple
    {
        public VerbTuple() : base(TupleDefinitions.Verb, null, null)
        {
        }

        public VerbTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Verb, sourceLineNumber, id)
        {
        }

        public IntermediateField this[VerbTupleFields index] => this.Fields[(int)index];

        public string ExtensionRef
        {
            get => (string)this.Fields[(int)VerbTupleFields.ExtensionRef];
            set => this.Set((int)VerbTupleFields.ExtensionRef, value);
        }

        public string Verb
        {
            get => (string)this.Fields[(int)VerbTupleFields.Verb];
            set => this.Set((int)VerbTupleFields.Verb, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)VerbTupleFields.Sequence];
            set => this.Set((int)VerbTupleFields.Sequence, value);
        }

        public string Command
        {
            get => (string)this.Fields[(int)VerbTupleFields.Command];
            set => this.Set((int)VerbTupleFields.Command, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)VerbTupleFields.Argument];
            set => this.Set((int)VerbTupleFields.Argument, value);
        }
    }
}