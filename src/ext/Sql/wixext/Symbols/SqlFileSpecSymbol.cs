// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Symbols;

    public static partial class SqlSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SqlFileSpec = new IntermediateSymbolDefinition(
            SqlSymbolDefinitionType.SqlFileSpec.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlFileSpecSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecSymbolFields.Filename), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecSymbolFields.Size), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecSymbolFields.MaxSize), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecSymbolFields.GrowthSize), IntermediateFieldType.String),
            },
            typeof(SqlFileSpecSymbol));
    }
}

namespace WixToolset.Sql.Symbols
{
    using WixToolset.Data;

    public enum SqlFileSpecSymbolFields
    {
        Name,
        Filename,
        Size,
        MaxSize,
        GrowthSize,
    }

    public class SqlFileSpecSymbol : IntermediateSymbol
    {
        public SqlFileSpecSymbol() : base(SqlSymbolDefinitions.SqlFileSpec, null, null)
        {
        }

        public SqlFileSpecSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlSymbolDefinitions.SqlFileSpec, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlFileSpecSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)SqlFileSpecSymbolFields.Name].AsString();
            set => this.Set((int)SqlFileSpecSymbolFields.Name, value);
        }

        public string Filename
        {
            get => this.Fields[(int)SqlFileSpecSymbolFields.Filename].AsString();
            set => this.Set((int)SqlFileSpecSymbolFields.Filename, value);
        }

        public string Size
        {
            get => this.Fields[(int)SqlFileSpecSymbolFields.Size].AsString();
            set => this.Set((int)SqlFileSpecSymbolFields.Size, value);
        }

        public string MaxSize
        {
            get => this.Fields[(int)SqlFileSpecSymbolFields.MaxSize].AsString();
            set => this.Set((int)SqlFileSpecSymbolFields.MaxSize, value);
        }

        public string GrowthSize
        {
            get => this.Fields[(int)SqlFileSpecSymbolFields.GrowthSize].AsString();
            set => this.Set((int)SqlFileSpecSymbolFields.GrowthSize, value);
        }
    }
}