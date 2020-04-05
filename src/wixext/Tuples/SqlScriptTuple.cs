// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Tuples;

    public static partial class SqlTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SqlScript = new IntermediateTupleDefinition(
            SqlTupleDefinitionType.SqlScript.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.SqlDbRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.ScriptBinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(SqlScriptTuple));
    }
}

namespace WixToolset.Sql.Tuples
{
    using WixToolset.Data;

    public enum SqlScriptTupleFields
    {
        SqlDbRef,
        ComponentRef,
        ScriptBinaryRef,
        UserRef,
        Attributes,
        Sequence,
    }

    public class SqlScriptTuple : IntermediateTuple
    {
        public SqlScriptTuple() : base(SqlTupleDefinitions.SqlScript, null, null)
        {
        }

        public SqlScriptTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlTupleDefinitions.SqlScript, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlScriptTupleFields index] => this.Fields[(int)index];

        public string SqlDbRef
        {
            get => this.Fields[(int)SqlScriptTupleFields.SqlDbRef].AsString();
            set => this.Set((int)SqlScriptTupleFields.SqlDbRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SqlScriptTupleFields.ComponentRef].AsString();
            set => this.Set((int)SqlScriptTupleFields.ComponentRef, value);
        }

        public string ScriptBinaryRef
        {
            get => this.Fields[(int)SqlScriptTupleFields.ScriptBinaryRef].AsString();
            set => this.Set((int)SqlScriptTupleFields.ScriptBinaryRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlScriptTupleFields.UserRef].AsString();
            set => this.Set((int)SqlScriptTupleFields.UserRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlScriptTupleFields.Attributes].AsNumber();
            set => this.Set((int)SqlScriptTupleFields.Attributes, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)SqlScriptTupleFields.Sequence].AsNumber();
            set => this.Set((int)SqlScriptTupleFields.Sequence, value);
        }
    }
}