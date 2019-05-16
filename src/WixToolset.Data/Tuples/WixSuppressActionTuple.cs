// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixSuppressAction = new IntermediateTupleDefinition(
            TupleDefinitionType.WixSuppressAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSuppressActionTupleFields.SequenceTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSuppressActionTupleFields.Action), IntermediateFieldType.String),
            },
            typeof(WixSuppressActionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixSuppressActionTupleFields
    {
        SequenceTable,
        Action,
    }

    public class WixSuppressActionTuple : IntermediateTuple
    {
        public WixSuppressActionTuple() : base(TupleDefinitions.WixSuppressAction, null, null)
        {
        }

        public WixSuppressActionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixSuppressAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSuppressActionTupleFields index] => this.Fields[(int)index];

        public SequenceTable SequenceTable
        {
            get => (SequenceTable)Enum.Parse(typeof(SequenceTable), (string)this.Fields[(int)WixSuppressActionTupleFields.SequenceTable]);
            set => this.Set((int)WixSuppressActionTupleFields.SequenceTable, value.ToString());
        }

        public string Action
        {
            get => (string)this.Fields[(int)WixSuppressActionTupleFields.Action];
            set => this.Set((int)WixSuppressActionTupleFields.Action, value);
        }
    }
}