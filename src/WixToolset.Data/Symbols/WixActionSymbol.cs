// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixAction = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.SequenceTable), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.Before), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.After), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionSymbolFields.Overridable), IntermediateFieldType.Bool),
            },
            typeof(WixActionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixActionSymbolFields
    {
        SequenceTable,
        Action,
        Condition,
        Sequence,
        Before,
        After,
        Overridable,
    }

    public enum SequenceTable
    {
        AdminUISequence,
        AdminExecuteSequence,
        AdvertiseExecuteSequence,
        InstallUISequence,
        InstallExecuteSequence
    }

    public class WixActionSymbol : IntermediateSymbol
    {
        public WixActionSymbol() : base(SymbolDefinitions.WixAction, null, null)
        {
        }

        public WixActionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixActionSymbolFields index] => this.Fields[(int)index];

        public SequenceTable SequenceTable
        {
            get => (SequenceTable)this.Fields[(int)WixActionSymbolFields.SequenceTable].AsNumber();
            set => this.Set((int)WixActionSymbolFields.SequenceTable, (int)value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)WixActionSymbolFields.Action];
            set => this.Set((int)WixActionSymbolFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixActionSymbolFields.Condition];
            set => this.Set((int)WixActionSymbolFields.Condition, value);
        }

        public int? Sequence
        {
            get => (int?)this.Fields[(int)WixActionSymbolFields.Sequence];
            set => this.Set((int)WixActionSymbolFields.Sequence, value);
        }

        public string Before
        {
            get => (string)this.Fields[(int)WixActionSymbolFields.Before];
            set => this.Set((int)WixActionSymbolFields.Before, value);
        }

        public string After
        {
            get => (string)this.Fields[(int)WixActionSymbolFields.After];
            set => this.Set((int)WixActionSymbolFields.After, value);
        }

        public bool Overridable
        {
            get => this.Fields[(int)WixActionSymbolFields.Overridable].AsBool();
            set => this.Set((int)WixActionSymbolFields.Overridable, value);
        }
    }
}
