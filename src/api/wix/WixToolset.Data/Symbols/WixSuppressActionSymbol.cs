// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSuppressAction = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSuppressAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSuppressActionSymbolFields.SequenceTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSuppressActionSymbolFields.Action), IntermediateFieldType.String),
            },
            typeof(WixSuppressActionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixSuppressActionSymbolFields
    {
        SequenceTable,
        Action,
    }

    public class WixSuppressActionSymbol : IntermediateSymbol
    {
        public WixSuppressActionSymbol() : base(SymbolDefinitions.WixSuppressAction, null, null)
        {
        }

        public WixSuppressActionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSuppressAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSuppressActionSymbolFields index] => this.Fields[(int)index];

        public SequenceTable SequenceTable
        {
            get => (SequenceTable)Enum.Parse(typeof(SequenceTable), (string)this.Fields[(int)WixSuppressActionSymbolFields.SequenceTable]);
            set => this.Set((int)WixSuppressActionSymbolFields.SequenceTable, value.ToString());
        }

        public string Action
        {
            get => (string)this.Fields[(int)WixSuppressActionSymbolFields.Action];
            set => this.Set((int)WixSuppressActionSymbolFields.Action, value);
        }
    }
}