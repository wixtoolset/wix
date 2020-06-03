// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixCustomTableColumn = new IntermediateTupleDefinition(
            TupleDefinitionType.WixCustomTableColumn,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.TableRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.MinValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.MaxValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.KeyTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.KeyColumn), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Category), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Set), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnTupleFields.Modularize), IntermediateFieldType.Number)
            },
            typeof(WixCustomTableColumnTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixCustomTableColumnTupleFields
    {
        TableRef,
        Name,
        Type,
        Attributes,
        Width,
        MinValue,
        MaxValue,
        KeyTable,
        KeyColumn,
        Category,
        Set,
        Description,
        Modularize,
    }

    [Flags]
    public enum WixCustomTableColumnTupleAttributes
    {
        None,
        PrimaryKey,
        Localizable,
        Nullable,
        Unreal,
    }

    public enum WixCustomTableColumnModularizeType
    {
        None,
        Column,
        CompanionFile,
        Condition,
        ControlEventArgument,
        ControlText,
        Icon,
        Property,
        SemicolonDelimited,
    }

    public class WixCustomTableColumnTuple : IntermediateTuple
    {
        public WixCustomTableColumnTuple() : base(TupleDefinitions.WixCustomTableColumn, null, null)
        {
        }

        public WixCustomTableColumnTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixCustomTableColumn, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableColumnTupleFields index] => this.Fields[(int)index];

        public string TableRef
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.TableRef];
            set => this.Set((int)WixCustomTableColumnTupleFields.TableRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.Name];
            set => this.Set((int)WixCustomTableColumnTupleFields.Name, value);
        }

        public IntermediateFieldType Type
        {
            get => (IntermediateFieldType)this.Fields[(int)WixCustomTableColumnTupleFields.Type].AsNumber();
            set => this.Set((int)WixCustomTableColumnTupleFields.Type, (int)value);
        }

        public WixCustomTableColumnTupleAttributes Attributes
        {
            get => (WixCustomTableColumnTupleAttributes)this.Fields[(int)WixCustomTableColumnTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixCustomTableColumnTupleFields.Attributes, (int)value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)WixCustomTableColumnTupleFields.Width];
            set => this.Set((int)WixCustomTableColumnTupleFields.Width, value);
        }

        public long? MinValue
        {
            get => (long?)this.Fields[(int)WixCustomTableColumnTupleFields.MinValue];
            set => this.Set((int)WixCustomTableColumnTupleFields.MinValue, value);
        }

        public long? MaxValue
        {
            get => (long?)this.Fields[(int)WixCustomTableColumnTupleFields.MaxValue];
            set => this.Set((int)WixCustomTableColumnTupleFields.MaxValue, value);
        }

        public string KeyTable
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.KeyTable];
            set => this.Set((int)WixCustomTableColumnTupleFields.KeyTable, value);
        }

        public int? KeyColumn
        {
            get => (int?)this.Fields[(int)WixCustomTableColumnTupleFields.KeyColumn];
            set => this.Set((int)WixCustomTableColumnTupleFields.KeyColumn, value);
        }

        public string Category
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.Category];
            set => this.Set((int)WixCustomTableColumnTupleFields.Category, value);
        }

        public string Set
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.Set];
            set => this.Set((int)WixCustomTableColumnTupleFields.Set, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixCustomTableColumnTupleFields.Description];
            set => this.Set((int)WixCustomTableColumnTupleFields.Description, value);
        }

        public WixCustomTableColumnModularizeType? Modularize
        {
            get => (WixCustomTableColumnModularizeType?)this.Fields[(int)WixCustomTableColumnTupleFields.Modularize].AsNullableNumber();
            set => this.Set((int)WixCustomTableColumnTupleFields.Modularize, (int?)value);
        }

        public bool PrimaryKey => (this.Attributes & WixCustomTableColumnTupleAttributes.PrimaryKey) == WixCustomTableColumnTupleAttributes.PrimaryKey;

        public bool Localizable => (this.Attributes & WixCustomTableColumnTupleAttributes.Localizable) == WixCustomTableColumnTupleAttributes.Localizable;

        public bool Nullable => (this.Attributes & WixCustomTableColumnTupleAttributes.Nullable) == WixCustomTableColumnTupleAttributes.Nullable;

        public bool Unreal => (this.Attributes & WixCustomTableColumnTupleAttributes.Unreal) == WixCustomTableColumnTupleAttributes.Unreal;
    }
}
