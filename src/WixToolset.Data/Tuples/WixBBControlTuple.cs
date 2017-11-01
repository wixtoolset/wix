// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBBControl = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBBControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBBControlTupleFields.Billboard_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBBControlTupleFields.BBControl_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBBControlTupleFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(WixBBControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBBControlTupleFields
    {
        Billboard_,
        BBControl_,
        SourceFile,
    }

    public class WixBBControlTuple : IntermediateTuple
    {
        public WixBBControlTuple() : base(TupleDefinitions.WixBBControl, null, null)
        {
        }

        public WixBBControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBBControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBBControlTupleFields index] => this.Fields[(int)index];

        public string Billboard_
        {
            get => (string)this.Fields[(int)WixBBControlTupleFields.Billboard_]?.Value;
            set => this.Set((int)WixBBControlTupleFields.Billboard_, value);
        }

        public string BBControl_
        {
            get => (string)this.Fields[(int)WixBBControlTupleFields.BBControl_]?.Value;
            set => this.Set((int)WixBBControlTupleFields.BBControl_, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)WixBBControlTupleFields.SourceFile]?.Value;
            set => this.Set((int)WixBBControlTupleFields.SourceFile, value);
        }
    }
}