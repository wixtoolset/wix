// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data.WindowsInstaller;

    public static class VSTableDefinitions
    {
        public static readonly TableDefinition HelpFile = new TableDefinition(
            "HelpFile",
            VSSymbolDefinitions.HelpFile,
            new[]
            {
                new ColumnDefinition("HelpFileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary Key for HelpFile Table (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HelpFileName", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Internal Microsoft Help ID for this HelpFile (required)."),
                new ColumnDefinition("LangID", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Language, description: "Language ID for content file (optional)."),
                new ColumnDefinition("File_HxS", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxS (Title) file (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_HxI", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxI (Index) file (optional).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_HxQ", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxQ (Query) file (optional).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_HxR", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxR (Attributes) file (optional).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_Samples", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for a file that is in the 'root' of the samples directory for this HelpFile (optional).", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition HelpFileToNamespace = new TableDefinition(
            "HelpFileToNamespace",
            VSSymbolDefinitions.HelpFileToNamespace,
            new[]
            {
                new ColumnDefinition("HelpFile_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpFile", keyColumn: 1, description: "Foreign key into HelpFile table (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table (required)."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition HelpFilter = new TableDefinition(
            "HelpFilter",
            VSSymbolDefinitions.HelpFilter,
            new[]
            {
                new ColumnDefinition("FilterKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary Key for HelpFilter (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Friendly name for Filter (required)."),
                new ColumnDefinition("QueryString", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Query String for Help Filter (optional)."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition HelpFilterToNamespace = new TableDefinition(
            "HelpFilterToNamespace",
            VSSymbolDefinitions.HelpFilterToNamespace,
            new[]
            {
                new ColumnDefinition("HelpFilter_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpFilter", keyColumn: 1, description: "Foreign key into HelpFilter table (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table (required)."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition HelpNamespace = new TableDefinition(
            "HelpNamespace",
            VSSymbolDefinitions.HelpNamespace,
            new[]
            {
                new ColumnDefinition("NamespaceKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary Key for HelpNamespace (required)."),
                new ColumnDefinition("NamespaceName", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Internal Microsoft Help ID for this Namespace (required)."),
                new ColumnDefinition("File_Collection", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxC (Collection) file (required).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Friendly name for Namespace (optional)."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition HelpPlugin = new TableDefinition(
            "HelpPlugin",
            VSSymbolDefinitions.HelpPlugin,
            new[]
            {
                new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table for the child namespace that will be plugged into the parent namespace (required)."),
                new ColumnDefinition("HelpNamespace_Parent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table for the parent namespace into which the child will be inserted (required)."),
                new ColumnDefinition("File_HxT", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxT  file of child namespace (optional).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_HxA", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxA (Attributes) file of child namespace (optional).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_ParentHxT", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Key for HxT  file of parent namespace that now includes the new child namespace (optional).", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            HelpFile,
            HelpFileToNamespace,
            HelpFilter,
            HelpFilterToNamespace,
            HelpNamespace,
            HelpPlugin,
        };
    }
}
