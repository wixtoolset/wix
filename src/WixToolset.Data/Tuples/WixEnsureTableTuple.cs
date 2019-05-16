// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixEnsureTable = new IntermediateTupleDefinition(
            TupleDefinitionType.WixEnsureTable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixEnsureTableTupleFields.Table), IntermediateFieldType.String),
            },
            typeof(WixEnsureTableTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixEnsureTableTupleFields
    {
        Table,
    }

    public class WixEnsureTableTuple : IntermediateTuple
    {
        public WixEnsureTableTuple() : base(TupleDefinitions.WixEnsureTable, null, null)
        {
        }

        public WixEnsureTableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixEnsureTable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixEnsureTableTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixEnsureTableTupleFields.Table];
            set => this.Set((int)WixEnsureTableTupleFields.Table, value);
        }
    }
}