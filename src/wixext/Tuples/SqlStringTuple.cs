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
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.SqlDbRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.SQL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlStringTupleFields.UserRef), IntermediateFieldType.String),
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
        SqlDbRef,
        ComponentRef,
        SQL,
        UserRef,
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

        public string SqlDbRef
        {
            get => this.Fields[(int)SqlStringTupleFields.SqlDbRef].AsString();
            set => this.Set((int)SqlStringTupleFields.SqlDbRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SqlStringTupleFields.ComponentRef].AsString();
            set => this.Set((int)SqlStringTupleFields.ComponentRef, value);
        }

        public string SQL
        {
            get => this.Fields[(int)SqlStringTupleFields.SQL].AsString();
            set => this.Set((int)SqlStringTupleFields.SQL, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlStringTupleFields.UserRef].AsString();
            set => this.Set((int)SqlStringTupleFields.UserRef, value);
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