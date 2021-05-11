// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Symbols;

    public static partial class SqlSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SqlString = new IntermediateSymbolDefinition(
            SqlSymbolDefinitionType.SqlString.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.SqlDbRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.SQL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SqlStringSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(SqlStringSymbol));
    }
}

namespace WixToolset.Sql.Symbols
{
    using WixToolset.Data;

    public enum SqlStringSymbolFields
    {
        SqlDbRef,
        ComponentRef,
        SQL,
        UserRef,
        Attributes,
        Sequence,
    }

    public class SqlStringSymbol : IntermediateSymbol
    {
        public SqlStringSymbol() : base(SqlSymbolDefinitions.SqlString, null, null)
        {
        }

        public SqlStringSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlSymbolDefinitions.SqlString, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlStringSymbolFields index] => this.Fields[(int)index];

        public string SqlDbRef
        {
            get => this.Fields[(int)SqlStringSymbolFields.SqlDbRef].AsString();
            set => this.Set((int)SqlStringSymbolFields.SqlDbRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SqlStringSymbolFields.ComponentRef].AsString();
            set => this.Set((int)SqlStringSymbolFields.ComponentRef, value);
        }

        public string SQL
        {
            get => this.Fields[(int)SqlStringSymbolFields.SQL].AsString();
            set => this.Set((int)SqlStringSymbolFields.SQL, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlStringSymbolFields.UserRef].AsString();
            set => this.Set((int)SqlStringSymbolFields.UserRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlStringSymbolFields.Attributes].AsNumber();
            set => this.Set((int)SqlStringSymbolFields.Attributes, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)SqlStringSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)SqlStringSymbolFields.Sequence, value);
        }
    }
}