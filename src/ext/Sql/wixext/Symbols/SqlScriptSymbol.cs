// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Symbols;

    public static partial class SqlSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SqlScript = new IntermediateSymbolDefinition(
            SqlSymbolDefinitionType.SqlScript.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.SqlDbRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.ScriptBinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SqlScriptSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(SqlScriptSymbol));
    }
}

namespace WixToolset.Sql.Symbols
{
    using WixToolset.Data;

    public enum SqlScriptSymbolFields
    {
        SqlDbRef,
        ComponentRef,
        ScriptBinaryRef,
        UserRef,
        Attributes,
        Sequence,
    }

    public class SqlScriptSymbol : IntermediateSymbol
    {
        public SqlScriptSymbol() : base(SqlSymbolDefinitions.SqlScript, null, null)
        {
        }

        public SqlScriptSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlSymbolDefinitions.SqlScript, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlScriptSymbolFields index] => this.Fields[(int)index];

        public string SqlDbRef
        {
            get => this.Fields[(int)SqlScriptSymbolFields.SqlDbRef].AsString();
            set => this.Set((int)SqlScriptSymbolFields.SqlDbRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SqlScriptSymbolFields.ComponentRef].AsString();
            set => this.Set((int)SqlScriptSymbolFields.ComponentRef, value);
        }

        public string ScriptBinaryRef
        {
            get => this.Fields[(int)SqlScriptSymbolFields.ScriptBinaryRef].AsString();
            set => this.Set((int)SqlScriptSymbolFields.ScriptBinaryRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlScriptSymbolFields.UserRef].AsString();
            set => this.Set((int)SqlScriptSymbolFields.UserRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlScriptSymbolFields.Attributes].AsNumber();
            set => this.Set((int)SqlScriptSymbolFields.Attributes, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)SqlScriptSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)SqlScriptSymbolFields.Sequence, value);
        }
    }
}