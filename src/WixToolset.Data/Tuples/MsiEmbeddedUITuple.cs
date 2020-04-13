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
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.EntryPoint), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.SupportsBasicUI), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.MessageFilter), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUITupleFields.Source), IntermediateFieldType.Path),
            },
            typeof(MsiEmbeddedUITuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiEmbeddedUITupleFields
    {
        FileName,
        EntryPoint,
        SupportsBasicUI,
        MessageFilter,
        Source,
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

        public string FileName
        {
            get => (string)this.Fields[(int)MsiEmbeddedUITupleFields.FileName];
            set => this.Set((int)MsiEmbeddedUITupleFields.FileName, value);
        }

        public bool EntryPoint
        {
            get => this.Fields[(int)MsiEmbeddedUITupleFields.EntryPoint].AsBool();
            set => this.Set((int)MsiEmbeddedUITupleFields.EntryPoint, value);
        }

        public bool SupportsBasicUI
        {
            get => this.Fields[(int)MsiEmbeddedUITupleFields.SupportsBasicUI].AsBool();
            set => this.Set((int)MsiEmbeddedUITupleFields.SupportsBasicUI, value);
        }

        public int? MessageFilter
        {
            get => (int?)this.Fields[(int)MsiEmbeddedUITupleFields.MessageFilter];
            set => this.Set((int)MsiEmbeddedUITupleFields.MessageFilter, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MsiEmbeddedUITupleFields.Source];
            set => this.Set((int)MsiEmbeddedUITupleFields.Source, value);
        }
    }
}
