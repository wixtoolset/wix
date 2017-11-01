// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition _Validation = new IntermediateTupleDefinition(
            TupleDefinitionType._Validation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Column), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Nullable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.MinValue), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.MaxValue), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.KeyTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.KeyColumn), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Category), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Set), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_ValidationTupleFields.Description), IntermediateFieldType.String),
            },
            typeof(_ValidationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum _ValidationTupleFields
    {
        Table,
        Column,
        Nullable,
        MinValue,
        MaxValue,
        KeyTable,
        KeyColumn,
        Category,
        Set,
        Description,
    }

    public class _ValidationTuple : IntermediateTuple
    {
        public _ValidationTuple() : base(TupleDefinitions._Validation, null, null)
        {
        }

        public _ValidationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions._Validation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[_ValidationTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Table]?.Value;
            set => this.Set((int)_ValidationTupleFields.Table, value);
        }

        public string Column
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Column]?.Value;
            set => this.Set((int)_ValidationTupleFields.Column, value);
        }

        public string Nullable
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Nullable]?.Value;
            set => this.Set((int)_ValidationTupleFields.Nullable, value);
        }

        public int MinValue
        {
            get => (int)this.Fields[(int)_ValidationTupleFields.MinValue]?.Value;
            set => this.Set((int)_ValidationTupleFields.MinValue, value);
        }

        public int MaxValue
        {
            get => (int)this.Fields[(int)_ValidationTupleFields.MaxValue]?.Value;
            set => this.Set((int)_ValidationTupleFields.MaxValue, value);
        }

        public string KeyTable
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.KeyTable]?.Value;
            set => this.Set((int)_ValidationTupleFields.KeyTable, value);
        }

        public int KeyColumn
        {
            get => (int)this.Fields[(int)_ValidationTupleFields.KeyColumn]?.Value;
            set => this.Set((int)_ValidationTupleFields.KeyColumn, value);
        }

        public string Category
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Category]?.Value;
            set => this.Set((int)_ValidationTupleFields.Category, value);
        }

        public string Set
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Set]?.Value;
            set => this.Set((int)_ValidationTupleFields.Set, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)_ValidationTupleFields.Description]?.Value;
            set => this.Set((int)_ValidationTupleFields.Description, value);
        }
    }
}