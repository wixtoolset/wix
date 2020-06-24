// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixCustomTableCell = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixCustomTableCell,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableCellSymbolFields.TableRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellSymbolFields.ColumnRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellSymbolFields.RowId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableCellSymbolFields.Data), IntermediateFieldType.String),
            },
            typeof(WixCustomTableCellSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixCustomTableCellSymbolFields
    {
        TableRef,
        ColumnRef,
        RowId,
        Data,
    }

    public class WixCustomTableCellSymbol : IntermediateSymbol
    {
        public WixCustomTableCellSymbol() : base(SymbolDefinitions.WixCustomTableCell, null, null)
        {
        }

        public WixCustomTableCellSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixCustomTableCell, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableCellSymbolFields index] => this.Fields[(int)index];

        public string TableRef
        {
            get => (string)this.Fields[(int)WixCustomTableCellSymbolFields.TableRef];
            set => this.Set((int)WixCustomTableCellSymbolFields.TableRef, value);
        }

        public string ColumnRef
        {
            get => (string)this.Fields[(int)WixCustomTableCellSymbolFields.ColumnRef];
            set => this.Set((int)WixCustomTableCellSymbolFields.ColumnRef, value);
        }

        public string RowId
        {
            get => (string)this.Fields[(int)WixCustomTableCellSymbolFields.RowId];
            set => this.Set((int)WixCustomTableCellSymbolFields.RowId, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)WixCustomTableCellSymbolFields.Data];
            set => this.Set((int)WixCustomTableCellSymbolFields.Data, value);
        }
    }
}
