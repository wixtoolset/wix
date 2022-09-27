// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    /// <summary>
    /// Creates section ids on rows which form logical groupings of resources.
    /// </summary>
    internal class GeneratePatchFilterIdsCommand
    {
        public GeneratePatchFilterIdsCommand(IReadOnlyCollection<IWindowsInstallerBackendBinderExtension> backendExtensions, WindowsInstallerData data, string filterIdPrefix)
        {
            this.BackendExtensions = backendExtensions;
            this.Data = data;
            this.FilterIdPrefix = filterIdPrefix;
        }

        private IReadOnlyCollection<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private WindowsInstallerData Data { get; }

        private string FilterIdPrefix { get; }

        public IDictionary<Row, string> RowToFilterId { get; private set; }

        public void Execute()
        {
            this.RowToFilterId = new Dictionary<Row, string>();

            var output = this.Data;

            // First assign and index section ids for the tables that are in their own sections.
            this.AssignFilterIdsToTable(output.Tables["Binary"], 0);
            var componentSectionIdIndex = this.AssignFilterIdsToTable(output.Tables["Component"], 0);
            var customActionSectionIdIndex = this.AssignFilterIdsToTable(output.Tables["CustomAction"], 0);
            this.AssignFilterIdsToTable(output.Tables["Directory"], 0);
            var featureSectionIdIndex = this.AssignFilterIdsToTable(output.Tables["Feature"], 0);
            this.AssignFilterIdsToTable(output.Tables["Icon"], 0);
            var digitalCertificateSectionIdIndex = this.AssignFilterIdsToTable(output.Tables["MsiDigitalCertificate"], 0);
            this.AssignFilterIdsToTable(output.Tables["Property"], 0);

            // Now handle all the tables that rely on the first set of indexes but also produce their own indexes. Order matters here.
            var fileFilterIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["File"], componentSectionIdIndex, 1, 0);
            var appIdFilterIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["Class"], componentSectionIdIndex, 2, 5);
            var odbcDataSourceFilterIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["ODBCDataSource"], componentSectionIdIndex, 1, 0);
            var odbcDriverSectionIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["ODBCDriver"], componentSectionIdIndex, 1, 0);
            var registrySectionIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["Registry"], componentSectionIdIndex, 5, 0);
            var serviceInstallSectionIdIndex = this.ConnectTableToSectionAndIndex(output.Tables["ServiceInstall"], componentSectionIdIndex, 11, 0);

            // Now handle all the tables which only rely on previous indexes and order does not matter.
            foreach (var table in output.Tables)
            {
                switch (table.Name)
                {
                    case "MsiFileHash":
                        this.ConnectTableToFilterId(table, fileFilterIdIndex, 0);
                        break;
                    case "MsiAssembly":
                    case "MsiAssemblyName":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 0);
                        break;
                    case "MsiPackageCertificate":
                    case "MsiPatchCertificate":
                        this.ConnectTableToFilterId(table, digitalCertificateSectionIdIndex, 1);
                        break;
                    case "CreateFolder":
                    case "FeatureComponents":
                    case "MoveFile":
                    case "ReserveCost":
                    case "ODBCTranslator":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 1);
                        break;
                    case "TypeLib":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 2);
                        break;
                    case "Shortcut":
                    case "Environment":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 3);
                        break;
                    case "RemoveRegistry":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 4);
                        break;
                    case "ServiceControl":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 5);
                        break;
                    case "IniFile":
                    case "RemoveIniFile":
                        this.ConnectTableToFilterId(table, componentSectionIdIndex, 7);
                        break;
                    case "AppId":
                        this.ConnectTableToFilterId(table, appIdFilterIdIndex, 0);
                        break;
                    case "Condition":
                        this.ConnectTableToFilterId(table, featureSectionIdIndex, 0);
                        break;
                    case "ODBCSourceAttribute":
                        this.ConnectTableToFilterId(table, odbcDataSourceFilterIdIndex, 0);
                        break;
                    case "ODBCAttribute":
                        this.ConnectTableToFilterId(table, odbcDriverSectionIdIndex, 0);
                        break;
                    case "AdminExecuteSequence":
                    case "AdminUISequence":
                    case "AdvtExecuteSequence":
                    case "AdvtUISequence":
                    case "InstallExecuteSequence":
                    case "InstallUISequence":
                        this.ConnectTableToFilterId(table, customActionSectionIdIndex, 0);
                        break;
                    case "LockPermissions":
                    case "MsiLockPermissions":
                        foreach (var row in table.Rows)
                        {
                            var lockObject = row.FieldAsString(0);
                            var tableName = row.FieldAsString(1);

                            var filterId = String.Empty;
                            switch (tableName)
                            {
                                case "File":
                                    filterId = fileFilterIdIndex[lockObject];
                                    break;
                                case "Registry":
                                    filterId = registrySectionIdIndex[lockObject];
                                    break;
                                case "ServiceInstall":
                                    filterId = serviceInstallSectionIdIndex[lockObject];
                                    break;
                            }

                            if (!String.IsNullOrEmpty(filterId))
                            {
                                this.RowToFilterId.Add(row, filterId);
                            }
                        }
                        break;
                }
            }

            // Now pass the data to each backend extension to allow them to analyze the data and determine their proper filter ids.
            foreach (var extension in this.BackendExtensions)
            {
                extension.FinalizePatchFilterIds(this.Data, this.RowToFilterId, this.FilterIdPrefix);
            }
        }

        private Dictionary<string, string> AssignFilterIdsToTable(Table table, int rowPrimaryKeyIndex)
        {
            var primaryKeyToFilterId = new Dictionary<string, string>();

            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    var filterId = this.GetNewFilterId(row);

                    this.RowToFilterId.Add(row, filterId);

                    primaryKeyToFilterId.Add(row.FieldAsString(rowPrimaryKeyIndex), filterId);
                }
            }

            return primaryKeyToFilterId;
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="filterIdByPrimaryKey">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        private void ConnectTableToFilterId(Table table, Dictionary<string, string> filterIdByPrimaryKey, int rowIndex)
        {
            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    if (filterIdByPrimaryKey.TryGetValue(row.FieldAsString(rowIndex), out var filterId))
                    {
                        this.RowToFilterId.Add(row, filterId);
                    }
                }
            }
        }

        /// <summary>
        /// Connects a table's rows to a table with filter ids already assigned and produces an index for other tables to connect to it.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="filterIdsByPrimaryKey">A dictionary containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A dictionary containing the tables key for each row paired with its assigned section id.</returns>
        private Dictionary<string, string> ConnectTableToSectionAndIndex(Table table, Dictionary<string, string> filterIdsByPrimaryKey, int rowIndex, int rowPrimaryKeyIndex)
        {
            var newPrimaryKeyToSectionId = new Dictionary<string, string>();

            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    var foreignKey = row.FieldAsString(rowIndex);

                    if (!filterIdsByPrimaryKey.TryGetValue(foreignKey, out var filterId))
                    {
                        continue;
                    }

                    this.RowToFilterId.Add(row, filterId);

                    var primaryKey = row.FieldAsString(rowPrimaryKeyIndex);

                    if (!String.IsNullOrEmpty(primaryKey) && filterIdsByPrimaryKey.ContainsKey(primaryKey))
                    {
                        newPrimaryKeyToSectionId.Add(primaryKey, filterId);
                    }
                }
            }

            return newPrimaryKeyToSectionId;
        }

        private string GetNewFilterId(Row row)
        {
            return this.FilterIdPrefix + row.Number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
