// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System.Collections.Generic;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class VSWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = new[] {
            new TableDefinition(
                "HelpFile",
                new[]
                {
                    new ColumnDefinition("HelpFileKey", ColumnType.String, 72, true, false, ColumnCategory.Identifier, description: "Primary Key for HelpFile Table (required)."),
                    new ColumnDefinition("HelpFileName", ColumnType.String, 0, false, false, ColumnCategory.Text, description: "Internal Microsoft Help ID for this HelpFile (required)."),
                    new ColumnDefinition("LangID", ColumnType.Number, 2, false, true, ColumnCategory.Language, description: "Language ID for content file (optional)."),
                    new ColumnDefinition("File_HxS", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxS (Title) file (required)."),
                    new ColumnDefinition("File_HxI", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxI (Index) file (required)."),
                    new ColumnDefinition("File_HxQ", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxQ (Query) file (required)."),
                    new ColumnDefinition("File_HxR", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxR (Attributes) file (required)."),
                    new ColumnDefinition("File_Samples", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for a file that is in the 'root' of the samples directory for this HelpFile (optional)."),
                }
            ),
            new TableDefinition(
                "HelpFileToNamespace",
                new[]
                {
                    new ColumnDefinition("HelpFile_", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpFile", keyColumn: 1, description: "Foreign key into HelpFile table (required)."),
                    new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table (required)."),
                }
            ),
            new TableDefinition(
                "HelpFilter",
                new[]
                {
                    new ColumnDefinition("FilterKey", ColumnType.String, 72, true, false, ColumnCategory.Identifier, description: "Primary Key for HelpFilter (required)."),
                    new ColumnDefinition("Description", ColumnType.Localized, 0, false, false, ColumnCategory.Text, description: "Friendly name for Filter (required)."),
                    new ColumnDefinition("QueryString", ColumnType.String, 0, false, true, ColumnCategory.Text, description: "Query String for Help Filter (optional)."),
                }
            ),
            new TableDefinition(
                "HelpFilterToNamespace",
                new[]
                {
                    new ColumnDefinition("HelpFilter_", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpFilter", keyColumn: 1, description: "Foreign key into HelpFilter table (required)."),
                    new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table (required)."),
                }
            ),
            new TableDefinition(
                "HelpNamespace",
                new[]
                {
                    new ColumnDefinition("NamespaceKey", ColumnType.String, 72, true, false, ColumnCategory.Identifier, description: "Primary Key for HelpNamespace (required)."),
                    new ColumnDefinition("NamespaceName", ColumnType.String, 0, false, false, ColumnCategory.Text, description: "Internal Microsoft Help ID for this Namespace (required)."),
                    new ColumnDefinition("File_Collection", ColumnType.String, 72, false, false, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxC (Collection) file (required)."),
                    new ColumnDefinition("Description", ColumnType.Localized, 0, false, true, ColumnCategory.Text, description: "Friendly name for Namespace (optional)."),
                }
            ),
            new TableDefinition(
                "HelpPlugin",
                new[]
                {
                    new ColumnDefinition("HelpNamespace_", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table for the child namespace that will be plugged into the parent namespace (required)."),
                    new ColumnDefinition("HelpNamespace_Parent", ColumnType.String, 72, true, false, ColumnCategory.Identifier, keyTable: "HelpNamespace", keyColumn: 1, description: "Foreign key into HelpNamespace table for the parent namespace into which the child will be inserted (required)."),
                    new ColumnDefinition("File_HxT", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxT  file of child namespace (optional)."),
                    new ColumnDefinition("File_HxA", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxA (Attributes) file of child namespace (optional)."),
                    new ColumnDefinition("File_ParentHxT", ColumnType.String, 72, false, true, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "Key for HxT  file of parent namespace that now includes the new child namespace (optional)."),
                }
            ),
        };

        public override IEnumerable<TableDefinition> TableDefinitions => Tables;
    }
}
