// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Symbols;

    public static partial class SqlSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SqlDatabase = new IntermediateSymbolDefinition(
            SqlSymbolDefinitionType.SqlDatabase.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.Server), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.Instance), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.Database), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.FileSpecRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.LogFileSpecRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlDatabaseSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(SqlDatabaseSymbol));
    }
}

namespace WixToolset.Sql.Symbols
{
    using WixToolset.Data;

    public enum SqlDatabaseSymbolFields
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

    public class SqlDatabaseSymbol : IntermediateSymbol
    {
        public SqlDatabaseSymbol() : base(SqlSymbolDefinitions.SqlDatabase, null, null)
        {
        }

        public SqlDatabaseSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlSymbolDefinitions.SqlDatabase, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlDatabaseSymbolFields index] => this.Fields[(int)index];

        public string Server
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.Server].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.Server, value);
        }

        public string Instance
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.Instance].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.Instance, value);
        }

        public string Database
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.Database].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.Database, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.ComponentRef].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.UserRef].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.UserRef, value);
        }

        public string FileSpecRef
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.FileSpecRef].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.FileSpecRef, value);
        }

        public string LogFileSpecRef
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.LogFileSpecRef].AsString();
            set => this.Set((int)SqlDatabaseSymbolFields.LogFileSpecRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SqlDatabaseSymbolFields.Attributes].AsNumber();
            set => this.Set((int)SqlDatabaseSymbolFields.Attributes, value);
        }
    }
}