// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixControl = new IntermediateTupleDefinition(
            TupleDefinitionType.WixControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixControlTupleFields.Dialog_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixControlTupleFields.Control_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixControlTupleFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(WixControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixControlTupleFields
    {
        Dialog_,
        Control_,
        SourceFile,
    }

    public class WixControlTuple : IntermediateTuple
    {
        public WixControlTuple() : base(TupleDefinitions.WixControl, null, null)
        {
        }

        public WixControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixControlTupleFields index] => this.Fields[(int)index];

        public string Dialog_
        {
            get => (string)this.Fields[(int)WixControlTupleFields.Dialog_]?.Value;
            set => this.Set((int)WixControlTupleFields.Dialog_, value);
        }

        public string Control_
        {
            get => (string)this.Fields[(int)WixControlTupleFields.Control_]?.Value;
            set => this.Set((int)WixControlTupleFields.Control_, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)WixControlTupleFields.SourceFile]?.Value;
            set => this.Set((int)WixControlTupleFields.SourceFile, value);
        }
    }
}