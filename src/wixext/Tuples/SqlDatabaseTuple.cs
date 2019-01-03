// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Tuples;

    public static partial class SqlTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SqlDatabase = new IntermediateTupleDefinition(
            SqlTupleDefinitionType.SqlDatabase.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.SqlDb), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Server), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Instance), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Database), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.FileSpec_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.FileSpec_Log), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(SqlDatabaseTuple));
    }
}

namespace WixToolset.Sql.Tuples
{
    using WixToolset.Data;

    public enum SqlDatabaseTupleFields
    {
        SqlDb,
        Server,
        Instance,
        Database,
        Component_,
        User_,
        FileSpec_,
        FileSpec_Log,
        Attributes,
    }

    public class SqlDatabaseTuple : IntermediateTuple
    {
        public SqlDatabaseTuple() : base(SqlTupleDefinitions.SqlDatabase, null, null)
        {
        }

        public SqlDatabaseTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlTupleDefinitions.SqlDatabase, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlDatabaseTupleFields index] => this.Fields[(int)index];

        public string SqlDb
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.SqlDb].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.SqlDb, value);
        }

        public string Server
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Server].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.Server, value);
        }

        public string Instance
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Instance].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.Instance, value);
        }

        public string Database
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Database].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.Database, value);
        }

        public string Component_
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Component_].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.Component_, value);
        }

        public string User_
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.User_].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.User_, value);
        }

        public string FileSpec_
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.FileSpec_].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.FileSpec_, value);
        }

        public string FileSpec_Log
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.FileSpec_Log].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.FileSpec_Log, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Attributes].AsNumber();
            set => this.Set((int)SqlDatabaseTupleFields.Attributes, value);
        }
    }
}