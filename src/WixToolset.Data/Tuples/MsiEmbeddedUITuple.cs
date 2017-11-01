// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiEmbeddedUI = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiEmbeddedUI,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.MsiEmbeddedUI), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.MessageFilter), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.Data), IntermediateFieldType.Path),
            },
            typeof(MsiEmbeddedUITuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiEmbeddedUITupleFields
    {
        MsiEmbeddedUI,
        FileName,
        Attributes,
        MessageFilter,
        Data,
    }

    public class MsiEmbeddedUITuple : IntermediateTuple
    {
        public MsiEmbeddedUITuple() : base(TupleDefinitions.MsiEmbeddedUI, null, null)
        {
        }

        public MsiEmbeddedUITuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiEmbeddedUI, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiEmbeddedUITupleFields index] => this.Fields[(int)index];

        public string MsiEmbeddedUI
        {
            get => (string)this.Fields[(int)MsiEmbeddedUITupleFields.MsiEmbeddedUI]?.Value;
            set => this.Set((int)MsiEmbeddedUITupleFields.MsiEmbeddedUI, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)MsiEmbeddedUITupleFields.FileName]?.Value;
            set => this.Set((int)MsiEmbeddedUITupleFields.FileName, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)MsiEmbeddedUITupleFields.Attributes]?.Value;
            set => this.Set((int)MsiEmbeddedUITupleFields.Attributes, value);
        }

        public int MessageFilter
        {
            get => (int)this.Fields[(int)MsiEmbeddedUITupleFields.MessageFilter]?.Value;
            set => this.Set((int)MsiEmbeddedUITupleFields.MessageFilter, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)MsiEmbeddedUITupleFields.Data]?.Value;
            set => this.Set((int)MsiEmbeddedUITupleFields.Data, value);
        }
    }
}