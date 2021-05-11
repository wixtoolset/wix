// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixCustomTableColumn = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixCustomTableColumn,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.TableRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.MinValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.MaxValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.KeyTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.KeyColumn), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Category), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Set), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableColumnSymbolFields.Modularize), IntermediateFieldType.Number)
            },
            typeof(WixCustomTableColumnSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixCustomTableColumnSymbolFields
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
    public enum WixCustomTableColumnSymbolAttributes
    {
        None = 0x0,
        PrimaryKey = 0x1,
        Localizable = 0x2,
        Nullable = 0x4,
        Unreal = 0x8,
    }

    public enum WixCustomTableColumnCategoryType
    {
        Text,
        UpperCase,
        LowerCase,
        Integer,
        DoubleInteger,
        TimeDate,
        Identifier,
        Property,
        Filename,
        WildCardFilename,
        Path,
        Paths,
        AnyPath,
        DefaultDir,
        RegPath,
        Formatted,
        FormattedSddl,
        Template,
        Condition,
        Guid,
        Version,
        Language,
        Binary,
        CustomSource,
        Cabinet,
        Shortcut,
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

    public class WixCustomTableColumnSymbol : IntermediateSymbol
    {
        public WixCustomTableColumnSymbol() : base(SymbolDefinitions.WixCustomTableColumn, null, null)
        {
        }

        public WixCustomTableColumnSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixCustomTableColumn, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableColumnSymbolFields index] => this.Fields[(int)index];

        public string TableRef
        {
            get => (string)this.Fields[(int)WixCustomTableColumnSymbolFields.TableRef];
            set => this.Set((int)WixCustomTableColumnSymbolFields.TableRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixCustomTableColumnSymbolFields.Name];
            set => this.Set((int)WixCustomTableColumnSymbolFields.Name, value);
        }

        public IntermediateFieldType Type
        {
            get => (IntermediateFieldType)this.Fields[(int)WixCustomTableColumnSymbolFields.Type].AsNumber();
            set => this.Set((int)WixCustomTableColumnSymbolFields.Type, (int)value);
        }

        public WixCustomTableColumnSymbolAttributes Attributes
        {
            get => (WixCustomTableColumnSymbolAttributes)this.Fields[(int)WixCustomTableColumnSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixCustomTableColumnSymbolFields.Attributes, (int)value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)WixCustomTableColumnSymbolFields.Width];
            set => this.Set((int)WixCustomTableColumnSymbolFields.Width, value);
        }

        public long? MinValue
        {
            get => (long?)this.Fields[(int)WixCustomTableColumnSymbolFields.MinValue];
            set => this.Set((int)WixCustomTableColumnSymbolFields.MinValue, value);
        }

        public long? MaxValue
        {
            get => (long?)this.Fields[(int)WixCustomTableColumnSymbolFields.MaxValue];
            set => this.Set((int)WixCustomTableColumnSymbolFields.MaxValue, value);
        }

        public string KeyTable
        {
            get => (string)this.Fields[(int)WixCustomTableColumnSymbolFields.KeyTable];
            set => this.Set((int)WixCustomTableColumnSymbolFields.KeyTable, value);
        }

        public int? KeyColumn
        {
            get => (int?)this.Fields[(int)WixCustomTableColumnSymbolFields.KeyColumn];
            set => this.Set((int)WixCustomTableColumnSymbolFields.KeyColumn, value);
        }

        public WixCustomTableColumnCategoryType? Category
        {
            get => (WixCustomTableColumnCategoryType?)this.Fields[(int)WixCustomTableColumnSymbolFields.Category].AsNullableNumber();
            set => this.Set((int)WixCustomTableColumnSymbolFields.Category, (int?)value);
        }

        public string Set
        {
            get => (string)this.Fields[(int)WixCustomTableColumnSymbolFields.Set];
            set => this.Set((int)WixCustomTableColumnSymbolFields.Set, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixCustomTableColumnSymbolFields.Description];
            set => this.Set((int)WixCustomTableColumnSymbolFields.Description, value);
        }

        public WixCustomTableColumnModularizeType? Modularize
        {
            get => (WixCustomTableColumnModularizeType?)this.Fields[(int)WixCustomTableColumnSymbolFields.Modularize].AsNullableNumber();
            set => this.Set((int)WixCustomTableColumnSymbolFields.Modularize, (int?)value);
        }

        public bool PrimaryKey => (this.Attributes & WixCustomTableColumnSymbolAttributes.PrimaryKey) == WixCustomTableColumnSymbolAttributes.PrimaryKey;

        public bool Localizable => (this.Attributes & WixCustomTableColumnSymbolAttributes.Localizable) == WixCustomTableColumnSymbolAttributes.Localizable;

        public bool Nullable => (this.Attributes & WixCustomTableColumnSymbolAttributes.Nullable) == WixCustomTableColumnSymbolAttributes.Nullable;

        public bool Unreal => (this.Attributes & WixCustomTableColumnSymbolAttributes.Unreal) == WixCustomTableColumnSymbolAttributes.Unreal;
    }
}
