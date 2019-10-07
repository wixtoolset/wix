// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Signature = new IntermediateTupleDefinition(
            TupleDefinitionType.Signature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MinSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MaxSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MinDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.MaxDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureTupleFields.Languages), IntermediateFieldType.String),
            },
            typeof(SignatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum SignatureTupleFields
    {
        FileName,
        MinVersion,
        MaxVersion,
        MinSize,
        MaxSize,
        MinDate,
        MaxDate,
        Languages,
    }

    public class SignatureTuple : IntermediateTuple
    {
        public SignatureTuple() : base(TupleDefinitions.Signature, null, null)
        {
        }

        public SignatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Signature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SignatureTupleFields index] => this.Fields[(int)index];

        public string FileName
        {
            get => (string)this.Fields[(int)SignatureTupleFields.FileName];
            set => this.Set((int)SignatureTupleFields.FileName, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)SignatureTupleFields.MinVersion];
            set => this.Set((int)SignatureTupleFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)SignatureTupleFields.MaxVersion];
            set => this.Set((int)SignatureTupleFields.MaxVersion, value);
        }

        public int MinSize
        {
            get => (int)this.Fields[(int)SignatureTupleFields.MinSize];
            set => this.Set((int)SignatureTupleFields.MinSize, value);
        }

        public int MaxSize
        {
            get => (int)this.Fields[(int)SignatureTupleFields.MaxSize];
            set => this.Set((int)SignatureTupleFields.MaxSize, value);
        }

        public int MinDate
        {
            get => (int)this.Fields[(int)SignatureTupleFields.MinDate];
            set => this.Set((int)SignatureTupleFields.MinDate, value);
        }

        public int MaxDate
        {
            get => (int)this.Fields[(int)SignatureTupleFields.MaxDate];
            set => this.Set((int)SignatureTupleFields.MaxDate, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)SignatureTupleFields.Languages];
            set => this.Set((int)SignatureTupleFields.Languages, value);
        }
    }
}