// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ReserveCost = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ReserveCost,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ReserveCostSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ReserveCostSymbolFields.ReserveFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ReserveCostSymbolFields.ReserveLocal), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ReserveCostSymbolFields.ReserveSource), IntermediateFieldType.Number),
            },
            typeof(ReserveCostSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ReserveCostSymbolFields
    {
        ComponentRef,
        ReserveFolder,
        ReserveLocal,
        ReserveSource,
    }

    public class ReserveCostSymbol : IntermediateSymbol
    {
        public ReserveCostSymbol() : base(SymbolDefinitions.ReserveCost, null, null)
        {
        }

        public ReserveCostSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ReserveCost, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ReserveCostSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ReserveCostSymbolFields.ComponentRef];
            set => this.Set((int)ReserveCostSymbolFields.ComponentRef, value);
        }

        public string ReserveFolder
        {
            get => (string)this.Fields[(int)ReserveCostSymbolFields.ReserveFolder];
            set => this.Set((int)ReserveCostSymbolFields.ReserveFolder, value);
        }

        public int ReserveLocal
        {
            get => (int)this.Fields[(int)ReserveCostSymbolFields.ReserveLocal];
            set => this.Set((int)ReserveCostSymbolFields.ReserveLocal, value);
        }

        public int ReserveSource
        {
            get => (int)this.Fields[(int)ReserveCostSymbolFields.ReserveSource];
            set => this.Set((int)ReserveCostSymbolFields.ReserveSource, value);
        }
    }
}
