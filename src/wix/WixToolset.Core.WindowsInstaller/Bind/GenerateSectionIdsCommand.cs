// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Creates section ids on rows which form logical groupings of resources.
    /// </summary>
    internal class GenerateSectionIdsCommand
    {
        private int sectionCount;

        public GenerateSectionIdsCommand(WindowsInstallerData data)
        {
            this.Data = data;
        }

        private WindowsInstallerData Data { get; }

        public void Execute()
        {
            var output = this.Data;

            this.sectionCount = 0;

            // First assign and index section ids for the tables that are in their own sections.
            this.AssignSectionIdsToTable(output.Tables["Binary"], 0);
            var componentSectionIdIndex = this.AssignSectionIdsToTable(output.Tables["Component"], 0);
            var customActionSectionIdIndex = this.AssignSectionIdsToTable(output.Tables["CustomAction"], 0);
            this.AssignSectionIdsToTable(output.Tables["Directory"], 0);
            var featureSectionIdIndex = this.AssignSectionIdsToTable(output.Tables["Feature"], 0);
            this.AssignSectionIdsToTable(output.Tables["Icon"], 0);
            var digitalCertificateSectionIdIndex = this.AssignSectionIdsToTable(output.Tables["MsiDigitalCertificate"], 0);
            this.AssignSectionIdsToTable(output.Tables["Property"], 0);

            // Now handle all the tables that rely on the first set of indexes but also produce their own indexes. Order matters here.
            var fileSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["File"], componentSectionIdIndex, 1, 0);
            var appIdSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["Class"], componentSectionIdIndex, 2, 5);
            var odbcDataSourceSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ODBCDataSource"], componentSectionIdIndex, 1, 0);
            var odbcDriverSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ODBCDriver"], componentSectionIdIndex, 1, 0);
            var registrySectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["Registry"], componentSectionIdIndex, 5, 0);
            var serviceInstallSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ServiceInstall"], componentSectionIdIndex, 11, 0);

            // Now handle all the tables which only rely on previous indexes and order does not matter.
            foreach (var table in output.Tables)
            {
                switch (table.Name)
                {
                    case "MsiFileHash":
                        ConnectTableToSection(table, fileSectionIdIndex, 0);
                        break;
                    case "MsiAssembly":
                    case "MsiAssemblyName":
                        ConnectTableToSection(table, componentSectionIdIndex, 0);
                        break;
                    case "MsiPackageCertificate":
                    case "MsiPatchCertificate":
                        ConnectTableToSection(table, digitalCertificateSectionIdIndex, 1);
                        break;
                    case "CreateFolder":
                    case "FeatureComponents":
                    case "MoveFile":
                    case "ReserveCost":
                    case "ODBCTranslator":
                        ConnectTableToSection(table, componentSectionIdIndex, 1);
                        break;
                    case "TypeLib":
                        ConnectTableToSection(table, componentSectionIdIndex, 2);
                        break;
                    case "Shortcut":
                    case "Environment":
                        ConnectTableToSection(table, componentSectionIdIndex, 3);
                        break;
                    case "RemoveRegistry":
                        ConnectTableToSection(table, componentSectionIdIndex, 4);
                        break;
                    case "ServiceControl":
                        ConnectTableToSection(table, componentSectionIdIndex, 5);
                        break;
                    case "IniFile":
                    case "RemoveIniFile":
                        ConnectTableToSection(table, componentSectionIdIndex, 7);
                        break;
                    case "AppId":
                        ConnectTableToSection(table, appIdSectionIdIndex, 0);
                        break;
                    case "Condition":
                        ConnectTableToSection(table, featureSectionIdIndex, 0);
                        break;
                    case "ODBCSourceAttribute":
                        ConnectTableToSection(table, odbcDataSourceSectionIdIndex, 0);
                        break;
                    case "ODBCAttribute":
                        ConnectTableToSection(table, odbcDriverSectionIdIndex, 0);
                        break;
                    case "AdminExecuteSequence":
                    case "AdminUISequence":
                    case "AdvtExecuteSequence":
                    case "AdvtUISequence":
                    case "InstallExecuteSequence":
                    case "InstallUISequence":
                        ConnectTableToSection(table, customActionSectionIdIndex, 0);
                        break;
                    case "LockPermissions":
                    case "MsiLockPermissions":
                        foreach (var row in table.Rows)
                        {
                            var lockObject = row.FieldAsString(0);
                            var tableName = row.FieldAsString(1);
                            switch (tableName)
                            {
                                case "File":
                                    row.SectionId = fileSectionIdIndex[lockObject];
                                    break;
                                case "Registry":
                                    row.SectionId = registrySectionIdIndex[lockObject];
                                    break;
                                case "ServiceInstall":
                                    row.SectionId = serviceInstallSectionIdIndex[lockObject];
                                    break;
                            }
                        }
                        break;
                }
            }

            // Now pass the output to each unbinder extension to allow them to analyze the output and determine their proper section ids.
            //foreach (IUnbinderExtension extension in this.unbinderExtensions)
            //{
            //    extension.GenerateSectionIds(output);
            //}
        }

        /// <summary>
        /// Creates new section ids on all the rows in a table.
        /// </summary>
        /// <param name="table">The table to add sections to.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A dictionary containing the tables key for each row paired with its assigned section id.</returns>
        private Dictionary<string, string> AssignSectionIdsToTable(Table table, int rowPrimaryKeyIndex)
        {
            var primaryKeyToSectionId = new Dictionary<string, string>();

            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    row.SectionId = this.GetNewSectionId();

                    primaryKeyToSectionId.Add(row.FieldAsString(rowPrimaryKeyIndex), row.SectionId);
                }
            }

            return primaryKeyToSectionId;
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        private static void ConnectTableToSection(Table table, Dictionary<string, string> sectionIdIndex, int rowIndex)
        {
            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    if (sectionIdIndex.TryGetValue(row.FieldAsString(rowIndex), out var sectionId))
                    {
                        row.SectionId = sectionId;
                    }
                }
            }
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table and produces an index for other tables to connect to it.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A dictionary containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A dictionary containing the tables key for each row paired with its assigned section id.</returns>
        private static Dictionary<string, string> ConnectTableToSectionAndIndex(Table table, Dictionary<string, string> sectionIdIndex, int rowIndex, int rowPrimaryKeyIndex)
        {
            var newPrimaryKeyToSectionId = new Dictionary<string, string>();

            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    var foreignKey = row.FieldAsString(rowIndex);

                    if (!sectionIdIndex.TryGetValue(foreignKey, out var sectionId))
                    {
                        continue;
                    }

                    row.SectionId = sectionId;

                    var primaryKey = row.FieldAsString(rowPrimaryKeyIndex);

                    if (!String.IsNullOrEmpty(primaryKey) && sectionIdIndex.ContainsKey(primaryKey))
                    {
                        newPrimaryKeyToSectionId.Add(primaryKey, row.SectionId);
                    }
                }
            }

            return newPrimaryKeyToSectionId;
        }

        private string GetNewSectionId()
        {
            this.sectionCount++;

            return "wix.section." + this.sectionCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}
