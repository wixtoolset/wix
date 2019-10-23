// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixCustomRow = new IntermediateTupleDefinition(
            TupleDefinitionType.WixCustomRow,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomRowTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomRowTupleFields.FieldData), IntermediateFieldType.String),
            },
            typeof(WixCustomRowTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixCustomRowTupleFields
    {
        Table,
        FieldData,
    }

    public class WixCustomRowTuple : IntermediateTuple
    {
        public const char FieldSeparator = '\x85';

        public WixCustomRowTuple() : base(TupleDefinitions.WixCustomRow, null, null)
        {
        }

        public WixCustomRowTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixCustomRow, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomRowTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixCustomRowTupleFields.Table];
            set => this.Set((int)WixCustomRowTupleFields.Table, value);
        }

        public string FieldData
        {
            get => (string)this.Fields[(int)WixCustomRowTupleFields.FieldData];
            set => this.Set((int)WixCustomRowTupleFields.FieldData, value);
        }

        public string[] FieldDataSeparated => this.FieldData.Split(FieldSeparator);
    }
}
