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
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Server), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Instance), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.Database), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.FileSpecRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseTupleFields.LogFileSpecRef), IntermediateFieldType.String),
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
        Server,
        Instance,
        Database,
        ComponentRef,
        UserRef,
        FileSpecRef,
        LogFileSpecRef,
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

        public string ComponentRef
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.ComponentRef].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.UserRef].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.UserRef, value);
        }

        public string FileSpecRef
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.FileSpecRef].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.FileSpecRef, value);
        }

        public string LogFileSpecRef
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.LogFileSpecRef].AsString();
            set => this.Set((int)SqlDatabaseTupleFields.LogFileSpecRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlDatabaseTupleFields.Attributes].AsNumber();
            set => this.Set((int)SqlDatabaseTupleFields.Attributes, value);
        }
    }
}