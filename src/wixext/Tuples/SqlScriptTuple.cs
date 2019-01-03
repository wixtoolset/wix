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
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.Script), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.SqlDb_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.ScriptBinary_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlScriptTupleFields.User_), IntermediateFieldType.String),
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
        Script,
        SqlDb_,
        Component_,
        ScriptBinary_,
        User_,
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

        public string Script
        {
            get => this.Fields[(int)SqlScriptTupleFields.Script].AsString();
            set => this.Set((int)SqlScriptTupleFields.Script, value);
        }

        public string SqlDb_
        {
            get => this.Fields[(int)SqlScriptTupleFields.SqlDb_].AsString();
            set => this.Set((int)SqlScriptTupleFields.SqlDb_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)SqlScriptTupleFields.Component_].AsString();
            set => this.Set((int)SqlScriptTupleFields.Component_, value);
        }

        public string ScriptBinary_
        {
            get => this.Fields[(int)SqlScriptTupleFields.ScriptBinary_].AsString();
            set => this.Set((int)SqlScriptTupleFields.ScriptBinary_, value);
        }

        public string User_
        {
            get => this.Fields[(int)SqlScriptTupleFields.User_].AsString();
            set => this.Set((int)SqlScriptTupleFields.User_, value);
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