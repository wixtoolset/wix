// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixCustomTable = new IntermediateTupleDefinition(
            TupleDefinitionType.WixCustomTable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableTupleFields.ColumnNames), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableTupleFields.Unreal), IntermediateFieldType.Bool),
            },
            typeof(WixCustomTableTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixCustomTableTupleFields
    {
        ColumnNames,
        Unreal,
    }

    public class WixCustomTableTuple : IntermediateTuple
    {
        public const char ColumnNamesSeparator = '\x85';

        public WixCustomTableTuple() : base(TupleDefinitions.WixCustomTable, null, null)
        {
        }

        public WixCustomTableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixCustomTable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableTupleFields index] => this.Fields[(int)index];

        public string ColumnNames
        {
            get => (string)this.Fields[(int)WixCustomTableTupleFields.ColumnNames];
            set => this.Set((int)WixCustomTableTupleFields.ColumnNames, value);
        }

        public bool Unreal
        {
            get => (bool)this.Fields[(int)WixCustomTableTupleFields.Unreal];
            set => this.Set((int)WixCustomTableTupleFields.Unreal, value);
        }

        public string[] ColumnNamesSeparated => this.ColumnNames.Split(ColumnNamesSeparator);
    }
}
