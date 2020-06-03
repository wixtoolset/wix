// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixCustomTableCell = new IntermediateTupleDefinition(
            TupleDefinitionType.WixCustomTableCell,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableCellTupleFields.TableRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellTupleFields.ColumnRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellTupleFields.RowId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellTupleFields.Data), IntermediateFieldType.String),
            },
            typeof(WixCustomTableCellTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixCustomTableCellTupleFields
    {
        TableRef,
        ColumnRef,
        RowId,
        Data,
    }

    public class WixCustomTableCellTuple : IntermediateTuple
    {
        public WixCustomTableCellTuple() : base(TupleDefinitions.WixCustomTableCell, null, null)
        {
        }

        public WixCustomTableCellTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixCustomTableCell, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableCellTupleFields index] => this.Fields[(int)index];

        public string TableRef
        {
            get => (string)this.Fields[(int)WixCustomTableCellTupleFields.TableRef];
            set => this.Set((int)WixCustomTableCellTupleFields.TableRef, value);
        }

        public string ColumnRef
        {
            get => (string)this.Fields[(int)WixCustomTableCellTupleFields.ColumnRef];
            set => this.Set((int)WixCustomTableCellTupleFields.ColumnRef, value);
        }

        public string RowId
        {
            get => (string)this.Fields[(int)WixCustomTableCellTupleFields.RowId];
            set => this.Set((int)WixCustomTableCellTupleFields.RowId, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)WixCustomTableCellTupleFields.Data];
            set => this.Set((int)WixCustomTableCellTupleFields.Data, value);
        }
    }
}
