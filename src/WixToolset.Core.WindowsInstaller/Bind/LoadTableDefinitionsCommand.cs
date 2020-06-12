// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class LoadTableDefinitionsCommand
    {
        public LoadTableDefinitionsCommand(IMessaging messaging, IntermediateSection section, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.BackendExtensions = backendExtensions;
        }

        public IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        public TableDefinitionCollection TableDefinitions { get; private set; }

        public TableDefinitionCollection Execute()
        {
            var tableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
            var customColumnsById = this.Section.Tuples.OfType<WixCustomTableColumnTuple>().ToDictionary(t => t.Id.Id);

            if (customColumnsById.Any())
            {
                foreach (var tuple in this.Section.Tuples.OfType<WixCustomTableTuple>())
                {
                    var customTableDefinition = this.CreateCustomTable(tuple, customColumnsById);
                    tableDefinitions.Add(customTableDefinition);
                }
            }

            foreach (var backendExtension in this.BackendExtensions)
            {
                foreach (var tableDefinition in backendExtension.TableDefinitions)
                {
                    if (tableDefinitions.Contains(tableDefinition.Name))
                    {
                        this.Messaging.Write(ErrorMessages.DuplicateExtensionTable(backendExtension.GetType().Assembly.Location, tableDefinition.Name));
                    }

                    tableDefinitions.Add(tableDefinition);
                }
            }

            this.TableDefinitions = tableDefinitions;
            return this.TableDefinitions;
        }

        private TableDefinition CreateCustomTable(WixCustomTableTuple tuple, Dictionary<string, WixCustomTableColumnTuple> customColumnsById)
        {
            var columnNames = tuple.ColumnNamesSeparated;
            var columns = new List<ColumnDefinition>(columnNames.Length);

            foreach (var name in columnNames)
            {
                var column = customColumnsById[tuple.Id.Id + "/" + name];

                var type = ColumnType.Unknown;

                if (column.Type == IntermediateFieldType.String)
                {
                    type = column.Localizable ? ColumnType.Localized : ColumnType.String;
                }
                else if (column.Type == IntermediateFieldType.Number)
                {
                    type = ColumnType.Number;
                }
                else if (column.Type == IntermediateFieldType.Path)
                {
                    type = ColumnType.Object;
                }

                var category = ColumnCategory.Unknown;
                switch (column.Category)
                {
                    case WixCustomTableColumnCategoryType.Text:
                        category = ColumnCategory.Text;
                        break;
                    case WixCustomTableColumnCategoryType.UpperCase:
                        category = ColumnCategory.UpperCase;
                        break;
                    case WixCustomTableColumnCategoryType.LowerCase:
                        category = ColumnCategory.LowerCase;
                        break;
                    case WixCustomTableColumnCategoryType.Integer:
                        category = ColumnCategory.Integer;
                        break;
                    case WixCustomTableColumnCategoryType.DoubleInteger:
                        category = ColumnCategory.DoubleInteger;
                        break;
                    case WixCustomTableColumnCategoryType.TimeDate:
                        category = ColumnCategory.TimeDate;
                        break;
                    case WixCustomTableColumnCategoryType.Identifier:
                        category = ColumnCategory.Identifier;
                        break;
                    case WixCustomTableColumnCategoryType.Property:
                        category = ColumnCategory.Property;
                        break;
                    case WixCustomTableColumnCategoryType.Filename:
                        category = ColumnCategory.Filename;
                        break;
                    case WixCustomTableColumnCategoryType.WildCardFilename:
                        category = ColumnCategory.WildCardFilename;
                        break;
                    case WixCustomTableColumnCategoryType.Path:
                        category = ColumnCategory.Path;
                        break;
                    case WixCustomTableColumnCategoryType.Paths:
                        category = ColumnCategory.Paths;
                        break;
                    case WixCustomTableColumnCategoryType.AnyPath:
                        category = ColumnCategory.AnyPath;
                        break;
                    case WixCustomTableColumnCategoryType.DefaultDir:
                        category = ColumnCategory.DefaultDir;
                        break;
                    case WixCustomTableColumnCategoryType.RegPath:
                        category = ColumnCategory.RegPath;
                        break;
                    case WixCustomTableColumnCategoryType.Formatted:
                        category = ColumnCategory.Formatted;
                        break;
                    case WixCustomTableColumnCategoryType.FormattedSddl:
                        category = ColumnCategory.FormattedSDDLText;
                        break;
                    case WixCustomTableColumnCategoryType.Template:
                        category = ColumnCategory.Template;
                        break;
                    case WixCustomTableColumnCategoryType.Condition:
                        category = ColumnCategory.Condition;
                        break;
                    case WixCustomTableColumnCategoryType.Guid:
                        category = ColumnCategory.Guid;
                        break;
                    case WixCustomTableColumnCategoryType.Version:
                        category = ColumnCategory.Version;
                        break;
                    case WixCustomTableColumnCategoryType.Language:
                        category = ColumnCategory.Language;
                        break;
                    case WixCustomTableColumnCategoryType.Binary:
                        category = ColumnCategory.Binary;
                        break;
                    case WixCustomTableColumnCategoryType.CustomSource:
                        category = ColumnCategory.CustomSource;
                        break;
                    case WixCustomTableColumnCategoryType.Cabinet:
                        category = ColumnCategory.Cabinet;
                        break;
                    case WixCustomTableColumnCategoryType.Shortcut:
                        category = ColumnCategory.Shortcut;
                        break;
                    case null:
                    default:
                        break;
                }

                var modularization = ColumnModularizeType.None;

                switch (column.Modularize)
                {
                    case null:
                    case WixCustomTableColumnModularizeType.None:
                        modularization = ColumnModularizeType.None;
                        break;
                    case WixCustomTableColumnModularizeType.Column:
                        modularization = ColumnModularizeType.Column;
                        break;
                    case WixCustomTableColumnModularizeType.CompanionFile:
                        modularization = ColumnModularizeType.CompanionFile;
                        break;
                    case WixCustomTableColumnModularizeType.Condition:
                        modularization = ColumnModularizeType.Condition;
                        break;
                    case WixCustomTableColumnModularizeType.ControlEventArgument:
                        modularization = ColumnModularizeType.ControlEventArgument;
                        break;
                    case WixCustomTableColumnModularizeType.ControlText:
                        modularization = ColumnModularizeType.ControlText;
                        break;
                    case WixCustomTableColumnModularizeType.Icon:
                        modularization = ColumnModularizeType.Icon;
                        break;
                    case WixCustomTableColumnModularizeType.Property:
                        modularization = ColumnModularizeType.Property;
                        break;
                    case WixCustomTableColumnModularizeType.SemicolonDelimited:
                        modularization = ColumnModularizeType.SemicolonDelimited;
                        break;
                }

                var columnDefinition = new ColumnDefinition(name, type, column.Width, column.PrimaryKey, column.Nullable, category, column.MinValue, column.MaxValue, column.KeyTable, column.KeyColumn, column.Set, column.Description, modularization, ColumnType.Localized == type, useCData: true, column.Unreal);
                columns.Add(columnDefinition);
            }

            var customTable = new TableDefinition(tuple.Id.Id, null, columns, tuple.Unreal);
            return customTable;
        }
    }
}
