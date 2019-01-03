// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Tuples;

    public static partial class SqlTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SqlString = new IntermediateTupleDefinition(
            SqlTupleDefinitionType.SqlString.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.String), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.SqlDb_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.SQL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(SqlStringTuple));
    }
}

namespace WixToolset.Sql.Tuples
{
    using WixToolset.Data;

    public enum SqlStringTupleFields
    {
        String,
        SqlDb_,
        Component_,
        SQL,
        User_,
        Attributes,
        Sequence,
    }

    public class SqlStringTuple : IntermediateTuple
    {
        public SqlStringTuple() : base(SqlTupleDefinitions.SqlString, null, null)
        {
        }

        public SqlStringTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlTupleDefinitions.SqlString, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlStringTupleFields index] => this.Fields[(int)index];

        public string String
        {
            get => this.Fields[(int)SqlStringTupleFields.String].AsString();
            set => this.Set((int)SqlStringTupleFields.String, value);
        }

        public string SqlDb_
        {
            get => this.Fields[(int)SqlStringTupleFields.SqlDb_].AsString();
            set => this.Set((int)SqlStringTupleFields.SqlDb_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)SqlStringTupleFields.Component_].AsString();
            set => this.Set((int)SqlStringTupleFields.Component_, value);
        }

        public string SQL
        {
            get => this.Fields[(int)SqlStringTupleFields.SQL].AsString();
            set => this.Set((int)SqlStringTupleFields.SQL, value);
        }

        public string User_
        {
            get => this.Fields[(int)SqlStringTupleFields.User_].AsString();
            set => this.Set((int)SqlStringTupleFields.User_, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlStringTupleFields.Attributes].AsNumber();
            set => this.Set((int)SqlStringTupleFields.Attributes, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)SqlStringTupleFields.Sequence].AsNumber();
            set => this.Set((int)SqlStringTupleFields.Sequence, value);
        }
    }
}