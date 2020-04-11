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

            foreach (var tuple in this.Section.Tuples.OfType<WixCustomTableTuple>())
            {
                var customTableDefinition = this.CreateCustomTable(tuple);
                tableDefinitions.Add(customTableDefinition);
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

        private TableDefinition CreateCustomTable(WixCustomTableTuple tuple)
        {
            var columnNames = tuple.ColumnNames.Split('\t');
            var columnTypes = tuple.ColumnTypes.Split('\t');
            var primaryKeys = tuple.PrimaryKeys.Split('\t');
            var minValues = tuple.MinValues?.Split('\t');
            var maxValues = tuple.MaxValues?.Split('\t');
            var keyTables = tuple.KeyTables?.Split('\t');
            var keyColumns = tuple.KeyColumns?.Split('\t');
            var categories = tuple.Categories?.Split('\t');
            var sets = tuple.Sets?.Split('\t');
            var descriptions = tuple.Descriptions?.Split('\t');
            var modularizations = tuple.Modularizations?.Split('\t');

            var currentPrimaryKey = 0;

            var columns = new List<ColumnDefinition>(columnNames.Length);
            for (var i = 0; i < columnNames.Length; ++i)
            {
                var name = columnNames[i];
                var type = ColumnType.Unknown;

                if (columnTypes[i].StartsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    type = ColumnType.String;
                }
                else if (columnTypes[i].StartsWith("l", StringComparison.OrdinalIgnoreCase))
                {
                    type = ColumnType.Localized;
                }
                else if (columnTypes[i].StartsWith("i", StringComparison.OrdinalIgnoreCase))
                {
                    type = ColumnType.Number;
                }
                else if (columnTypes[i].StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    type = ColumnType.Object;
                }

                var nullable = columnTypes[i].Substring(0, 1) == columnTypes[i].Substring(0, 1).ToUpperInvariant();
                var length = Convert.ToInt32(columnTypes[i].Substring(1), CultureInfo.InvariantCulture);

                var primaryKey = false;
                if (currentPrimaryKey < primaryKeys.Length && primaryKeys[currentPrimaryKey] == columnNames[i])
                {
                    primaryKey = true;
                    currentPrimaryKey++;
                }

                var minValue = String.IsNullOrEmpty(minValues?[i]) ? (int?)null : Convert.ToInt32(minValues[i], CultureInfo.InvariantCulture);
                var maxValue = String.IsNullOrEmpty(maxValues?[i]) ? (int?)null : Convert.ToInt32(maxValues[i], CultureInfo.InvariantCulture);
                var keyColumn = String.IsNullOrEmpty(keyColumns?[i]) ? (int?)null : Convert.ToInt32(keyColumns[i], CultureInfo.InvariantCulture);

                var category = ColumnCategory.Unknown;
                if (null != categories && null != categories[i] && 0 < categories[i].Length)
                {
                    switch (categories[i])
                    {
                        case "Text":
                            category = ColumnCategory.Text;
                            break;
                        case "UpperCase":
                            category = ColumnCategory.UpperCase;
                            break;
                        case "LowerCase":
                            category = ColumnCategory.LowerCase;
                            break;
                        case "Integer":
                            category = ColumnCategory.Integer;
                            break;
                        case "DoubleInteger":
                            category = ColumnCategory.DoubleInteger;
                            break;
                        case "TimeDate":
                            category = ColumnCategory.TimeDate;
                            break;
                        case "Identifier":
                            category = ColumnCategory.Identifier;
                            break;
                        case "Property":
                            category = ColumnCategory.Property;
                            break;
                        case "Filename":
                            category = ColumnCategory.Filename;
                            break;
                        case "WildCardFilename":
                            category = ColumnCategory.WildCardFilename;
                            break;
                        case "Path":
                            category = ColumnCategory.Path;
                            break;
                        case "Paths":
                            category = ColumnCategory.Paths;
                            break;
                        case "AnyPath":
                            category = ColumnCategory.AnyPath;
                            break;
                        case "DefaultDir":
                            category = ColumnCategory.DefaultDir;
                            break;
                        case "RegPath":
                            category = ColumnCategory.RegPath;
                            break;
                        case "Formatted":
                            category = ColumnCategory.Formatted;
                            break;
                        case "FormattedSddl":
                            category = ColumnCategory.FormattedSDDLText;
                            break;
                        case "Template":
                            category = ColumnCategory.Template;
                            break;
                        case "Condition":
                            category = ColumnCategory.Condition;
                            break;
                        case "Guid":
                            category = ColumnCategory.Guid;
                            break;
                        case "Version":
                            category = ColumnCategory.Version;
                            break;
                        case "Language":
                            category = ColumnCategory.Language;
                            break;
                        case "Binary":
                            category = ColumnCategory.Binary;
                            break;
                        case "CustomSource":
                            category = ColumnCategory.CustomSource;
                            break;
                        case "Cabinet":
                            category = ColumnCategory.Cabinet;
                            break;
                        case "Shortcut":
                            category = ColumnCategory.Shortcut;
                            break;
                        default:
                            break;
                    }
                }

                var keyTable = keyTables?[i];
                var setValue = sets?[i];
                var description = descriptions?[i];
                var modString = modularizations?[i];
                var modularization = ColumnModularizeType.None;

                switch (modString)
                {
                    case null:
                    case "None":
                        modularization = ColumnModularizeType.None;
                        break;
                    case "Column":
                        modularization = ColumnModularizeType.Column;
                        break;
                    case "Property":
                        modularization = ColumnModularizeType.Property;
                        break;
                    case "Condition":
                        modularization = ColumnModularizeType.Condition;
                        break;
                    case "CompanionFile":
                        modularization = ColumnModularizeType.CompanionFile;
                        break;
                    case "SemicolonDelimited":
                        modularization = ColumnModularizeType.SemicolonDelimited;
                        break;
                }

                var columnDefinition = new ColumnDefinition(name, type, length, primaryKey, nullable, category, minValue, maxValue, keyTable, keyColumn, setValue, description, modularization, ColumnType.Localized == type, true);
                columns.Add(columnDefinition);
            }

            var customTable = new TableDefinition(tuple.Id.Id, columns, tuple.Unreal);
            return customTable;
        }
    }
}
