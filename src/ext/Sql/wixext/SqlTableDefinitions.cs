// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data.WindowsInstaller;

    public static class SqlTableDefinitions
    {
        public static readonly TableDefinition SqlDatabase = new TableDefinition(
            "Wix4SqlDatabase",
            SqlSymbolDefinitions.SqlDatabase,
            new[]
            {
                new ColumnDefinition("SqlDb", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Server", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Primary key, name of server running SQL Server"),
                new ColumnDefinition("Instance", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Primary key, name of SQL Server instance"),
                new ColumnDefinition("Database", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Primary key, name of database in a SQL Server"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state ", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "User", keyColumn: 1, description: "Foreign key, User used to log into database", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileSpec_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Wix4SqlFileSpec", keyColumn: 1, description: "Foreign key referencing SqlFileSpec.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileSpec_Log", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Wix4SqlFileSpec", keyColumn: 1, description: "Foreign key referencing SqlFileSpec.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 255, description: "1 == create on install, 2 == drop on uninstall, 4 == continue on error, 8 == drop on install, 16 == create on uninstall, 32 == confirm update existing table, 64 == create on reinstall, 128 == drop on reinstall"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SqlFileSpec = new TableDefinition(
            "Wix4SqlFileSpec",
            SqlSymbolDefinitions.SqlFileSpec,
            new[]
            {
                new ColumnDefinition("FileSpec", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Logical name of filespec", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Filename", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Filename to use (path must exist)", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Size", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Initial size for file", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("MaxSize", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Maximum size for file", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("GrowthSize", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Size file should grow when necessary", modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SqlScript = new TableDefinition(
            "Wix4SqlScript",
            SqlSymbolDefinitions.SqlScript,
            new[]
            {
                new ColumnDefinition("Script", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token"),
                new ColumnDefinition("SqlDb_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4SqlDatabase", keyColumn: 1, description: "Foreign key, SQL Server key", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ScriptBinary_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Binary", keyColumn: 1, description: "Foreign key, Binary stream that contains SQL Script to execute", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "User", keyColumn: 1, description: "Foreign key, User used to log into database", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23;24;25;26;27;28;29;30;31", description: "1 == execute on install, 2 == execute on uninstall, 4 == continue on error, 8 == rollback on install, 16 == rollback on uninstall"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Order to execute SQL Queries in"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SqlString = new TableDefinition(
            "Wix4SqlString",
            SqlSymbolDefinitions.SqlString,
            new[]
            {
                new ColumnDefinition("String", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Id for the Wix4SqlString", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SqlDb_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4SqlDatabase", keyColumn: 1, description: "Foreign key, SQL Server key", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SQL", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "SQL query to execute"),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "User", keyColumn: 1, description: "Foreign key, User used to log into database", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23;24;25;26;27;28;29;30;31", description: "1 == execute on install, 2 == execute on uninstall, 4 == continue on error, 8 == rollback on install, 16 == rollback on uninstall"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Order to execute SQL Queries in"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            SqlDatabase,
            SqlFileSpec,
            SqlScript,
            SqlString,
        };
    }
}
