// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    internal class UnbindDatabaseCommand
    {
        private List<string> exportedFiles;

        public UnbindDatabaseCommand(IMessaging messaging, Database database, string databasePath, OutputType outputType, string exportBasePath, string intermediateFolder, bool isAdminImage, bool suppressDemodularization, bool skipSummaryInfo)
        {
            this.Messaging = messaging;
            this.Database = database;
            this.DatabasePath = databasePath;
            this.OutputType = outputType;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;
            this.IsAdminImage = isAdminImage;
            this.SuppressDemodularization = suppressDemodularization;
            this.SkipSummaryInfo = skipSummaryInfo;

            this.TableDefinitions = WindowsInstallerStandardInternal.GetTableDefinitions();
        }

        public IMessaging Messaging { get; }

        public Database Database { get; }

        public string DatabasePath { get; }

        public OutputType OutputType { get; }

        public string ExportBasePath { get; }

        public string IntermediateFolder { get; }

        public bool IsAdminImage { get; }

        public bool SuppressDemodularization { get; }

        public bool SkipSummaryInfo { get; }

        public TableDefinitionCollection TableDefinitions { get; }

        public IEnumerable<string> ExportedFiles => this.exportedFiles;

        private int SectionCount { get; set; }

        public WindowsInstallerData Execute()
        {
            this.exportedFiles = new List<string>();

            string modularizationGuid = null;
            var output = new WindowsInstallerData(new SourceLineNumber(this.DatabasePath));
            View validationView = null;

            // set the output type
            output.Type = this.OutputType;

            Directory.CreateDirectory(this.IntermediateFolder);

            // get the codepage
            this.Database.Export("_ForceCodepage", this.IntermediateFolder, "_ForceCodepage.idt");
            using (var sr = File.OpenText(Path.Combine(this.IntermediateFolder, "_ForceCodepage.idt")))
            {
                string line;

                while (null != (line = sr.ReadLine()))
                {
                    var data = line.Split('\t');

                    if (2 == data.Length)
                    {
                        output.Codepage = Convert.ToInt32(data[0], CultureInfo.InvariantCulture);
                    }
                }
            }

            // get the summary information table if it exists; it won't if unbinding a transform
            if (!this.SkipSummaryInfo)
            {
                using (var summaryInformation = new SummaryInformation(this.Database))
                {
                    var table = new Table(this.TableDefinitions["_SummaryInformation"]);

                    for (var i = 1; 19 >= i; i++)
                    {
                        var value = summaryInformation.GetProperty(i);

                        if (0 < value.Length)
                        {
                            var row = table.CreateRow(output.SourceLineNumbers);
                            row[0] = i;
                            row[1] = value;
                        }
                    }

                    output.Tables.Add(table);
                }
            }

            try
            {
                // open a view on the validation table if it exists
                if (this.Database.TableExists("_Validation"))
                {
                    validationView = this.Database.OpenView("SELECT * FROM `_Validation` WHERE `Table` = ? AND `Column` = ?");
                }

                // get the normal tables
                using (var tablesView = this.Database.OpenExecuteView("SELECT * FROM _Tables"))
                {
                    while (true)
                    {
                        using (var tableRecord = tablesView.Fetch())
                        {
                            if (null == tableRecord)
                            {
                                break;
                            }

                            var tableName = tableRecord.GetString(1);

                            using (var tableView = this.Database.OpenExecuteView(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM `{0}`", tableName)))
                            {
                                ColumnDefinition[] columns;
                                using (Record columnNameRecord = tableView.GetColumnInfo(MsiInterop.MSICOLINFONAMES),
                                              columnTypeRecord = tableView.GetColumnInfo(MsiInterop.MSICOLINFOTYPES))
                                {
                                    // index the primary keys
                                    var tablePrimaryKeys = new HashSet<string>();
                                    using (var primaryKeysRecord = this.Database.PrimaryKeys(tableName))
                                    {
                                        var primaryKeysFieldCount = primaryKeysRecord.GetFieldCount();

                                        for (var i = 1; i <= primaryKeysFieldCount; i++)
                                        {
                                            tablePrimaryKeys.Add(primaryKeysRecord.GetString(i));
                                        }
                                    }

                                    var columnCount = columnNameRecord.GetFieldCount();
                                    columns = new ColumnDefinition[columnCount];
                                    for (var i = 1; i <= columnCount; i++)
                                    {
                                        var columnName = columnNameRecord.GetString(i);
                                        var idtType = columnTypeRecord.GetString(i);

                                        ColumnType columnType;
                                        int length;
                                        bool nullable;

                                        var columnCategory = ColumnCategory.Unknown;
                                        var columnModularizeType = ColumnModularizeType.None;
                                        var primary = tablePrimaryKeys.Contains(columnName);
                                        int? minValue = null;
                                        int? maxValue = null;
                                        string keyTable = null;
                                        int? keyColumn = null;
                                        string category = null;
                                        string set = null;
                                        string description = null;

                                        // get the column type, length, and whether its nullable
                                        switch (Char.ToLower(idtType[0], CultureInfo.InvariantCulture))
                                        {
                                        case 'i':
                                            columnType = ColumnType.Number;
                                            break;
                                        case 'l':
                                            columnType = ColumnType.Localized;
                                            break;
                                        case 's':
                                            columnType = ColumnType.String;
                                            break;
                                        case 'v':
                                            columnType = ColumnType.Object;
                                            break;
                                        default:
                                            // TODO: error
                                            columnType = ColumnType.Unknown;
                                            break;
                                        }
                                        length = Convert.ToInt32(idtType.Substring(1), CultureInfo.InvariantCulture);
                                        nullable = Char.IsUpper(idtType[0]);

                                        // try to get validation information
                                        if (null != validationView)
                                        {
                                            using (var validationRecord = new Record(2))
                                            {
                                                validationRecord.SetString(1, tableName);
                                                validationRecord.SetString(2, columnName);

                                                validationView.Execute(validationRecord);
                                            }

                                            using (var validationRecord = validationView.Fetch())
                                            {
                                                if (null != validationRecord)
                                                {
                                                    var validationNullable = validationRecord.GetString(3);
                                                    minValue = validationRecord.IsNull(4) ? null : (int?)validationRecord.GetInteger(4);
                                                    maxValue = validationRecord.IsNull(5) ? null : (int?)validationRecord.GetInteger(5);
                                                    keyTable = validationRecord.IsNull(6) ? null : validationRecord.GetString(6);
                                                    keyColumn = validationRecord.IsNull(7) ? null : (int?)validationRecord.GetInteger(7);
                                                    category = validationRecord.IsNull(8) ? null : validationRecord.GetString(8);
                                                    set = validationRecord.IsNull(9) ? null : validationRecord.GetString(9);
                                                    description = validationRecord.IsNull(10) ? null : validationRecord.GetString(10);

                                                    // check the validation nullable value against the column definition
                                                    if (null == validationNullable)
                                                    {
                                                        // TODO: warn for illegal validation nullable column
                                                    }
                                                    else if ((nullable && "Y" != validationNullable) || (!nullable && "N" != validationNullable))
                                                    {
                                                        // TODO: warn for mismatch between column definition and validation nullable
                                                    }

                                                    // convert category to ColumnCategory
                                                    if (null != category)
                                                    {
                                                        try
                                                        {
                                                            columnCategory = (ColumnCategory)Enum.Parse(typeof(ColumnCategory), category, true);
                                                        }
                                                        catch (ArgumentException)
                                                        {
                                                            columnCategory = ColumnCategory.Unknown;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // TODO: warn about no validation information
                                                }
                                            }
                                        }

                                        // guess the modularization type
                                        if ("Icon" == keyTable && 1 == keyColumn)
                                        {
                                            columnModularizeType = ColumnModularizeType.Icon;
                                        }
                                        else if ("Condition" == columnName)
                                        {
                                            columnModularizeType = ColumnModularizeType.Condition;
                                        }
                                        else if (ColumnCategory.Formatted == columnCategory || ColumnCategory.FormattedSDDLText == columnCategory)
                                        {
                                            columnModularizeType = ColumnModularizeType.Property;
                                        }
                                        else if (ColumnCategory.Identifier == columnCategory)
                                        {
                                            columnModularizeType = ColumnModularizeType.Column;
                                        }

                                        columns[i - 1] = new ColumnDefinition(columnName, columnType, length, primary, nullable, columnCategory, minValue, maxValue, keyTable, keyColumn, set, description, columnModularizeType, (ColumnType.Localized == columnType), true);
                                    }
                                }

                                var tableDefinition = new TableDefinition(tableName, columns, false);

                                // use our table definitions if core properties are the same; this allows us to take advantage
                                // of wix concepts like localizable columns which current code assumes
                                if (this.TableDefinitions.Contains(tableName) && 0 == tableDefinition.CompareTo(this.TableDefinitions[tableName]))
                                {
                                    tableDefinition = this.TableDefinitions[tableName];
                                }

                                var table = new Table(tableDefinition);

                                while (true)
                                {
                                    using (var rowRecord = tableView.Fetch())
                                    {
                                        if (null == rowRecord)
                                        {
                                            break;
                                        }

                                        var recordCount = rowRecord.GetFieldCount();
                                        var row = table.CreateRow(output.SourceLineNumbers);

                                        for (var i = 0; recordCount > i && row.Fields.Length > i; i++)
                                        {
                                            if (rowRecord.IsNull(i + 1))
                                            {
                                                if (!row.Fields[i].Column.Nullable)
                                                {
                                                    // TODO: display an error for a null value in a non-nullable field OR
                                                    // display a warning and put an empty string in the value to let the compiler handle it
                                                    // (the second option is risky because the later code may make certain assumptions about
                                                    // the contents of a row value)
                                                }
                                            }
                                            else
                                            {
                                                switch (row.Fields[i].Column.Type)
                                                {
                                                case ColumnType.Number:
                                                    var success = false;
                                                    var intValue = rowRecord.GetInteger(i + 1);
                                                    if (row.Fields[i].Column.IsLocalizable)
                                                    {
                                                        success = row.BestEffortSetField(i, Convert.ToString(intValue, CultureInfo.InvariantCulture));
                                                    }
                                                    else
                                                    {
                                                        success = row.BestEffortSetField(i, intValue);
                                                    }

                                                    if (!success)
                                                    {
                                                        this.Messaging.Write(WarningMessages.BadColumnDataIgnored(row.SourceLineNumbers, Convert.ToString(intValue, CultureInfo.InvariantCulture), tableName, row.Fields[i].Column.Name));
                                                    }
                                                    break;
                                                case ColumnType.Object:
                                                    var sourceFile = "FILE NOT EXPORTED, USE THE dark.exe -x OPTION TO EXPORT BINARIES";

                                                    if (null != this.ExportBasePath)
                                                    {
                                                        var relativeSourceFile = Path.Combine(tableName, row.GetPrimaryKey('.'));
                                                        sourceFile = Path.Combine(this.ExportBasePath, relativeSourceFile);

                                                        // ensure the parent directory exists
                                                        System.IO.Directory.CreateDirectory(Path.Combine(this.ExportBasePath, tableName));

                                                        using (var fs = System.IO.File.Create(sourceFile))
                                                        {
                                                            int bytesRead;
                                                            var buffer = new byte[512];

                                                            while (0 != (bytesRead = rowRecord.GetStream(i + 1, buffer, buffer.Length)))
                                                            {
                                                                fs.Write(buffer, 0, bytesRead);
                                                            }
                                                        }

                                                        this.exportedFiles.Add(sourceFile);
                                                    }

                                                    row[i] = sourceFile;
                                                    break;
                                                default:
                                                    var value = rowRecord.GetString(i + 1);

                                                    switch (row.Fields[i].Column.Category)
                                                    {
                                                    case ColumnCategory.Guid:
                                                        value = value.ToUpper(CultureInfo.InvariantCulture);
                                                        break;
                                                    }

                                                    // de-modularize
                                                    if (!this.SuppressDemodularization && OutputType.Module == output.Type && ColumnModularizeType.None != row.Fields[i].Column.ModularizeType)
                                                    {
                                                        var modularization = new Regex(@"\.[0-9A-Fa-f]{8}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{12}");

                                                        if (null == modularizationGuid)
                                                        {
                                                            var match = modularization.Match(value);
                                                            if (match.Success)
                                                            {
                                                                modularizationGuid = String.Concat('{', match.Value.Substring(1).Replace('_', '-'), '}');
                                                            }
                                                        }

                                                        value = modularization.Replace(value, String.Empty);
                                                    }

                                                    // escape "$(" for the preprocessor
                                                    value = value.Replace("$(", "$$(");

                                                    // escape things that look like wix variables
                                                    var matches = Common.WixVariableRegex.Matches(value);
                                                    for (var j = matches.Count - 1; 0 <= j; j--)
                                                    {
                                                        value = value.Insert(matches[j].Index, "!");
                                                    }

                                                    row[i] = value;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                output.Tables.Add(table);
                            }

                        }
                    }
                }
            }
            finally
            {
                if (null != validationView)
                {
                    validationView.Close();
                }
            }

            // set the modularization guid as the PackageCode
            if (null != modularizationGuid)
            {
                var table = output.Tables["_SummaryInformation"];

                foreach (var row in table.Rows)
                {
                    if (9 == (int)row[0]) // PID_REVNUMBER
                    {
                        row[1] = modularizationGuid;
                    }
                }
            }

            if (this.IsAdminImage)
            {
                this.GenerateWixFileTable(this.DatabasePath, output);
                this.GenerateSectionIds(output);
            }

            return output;
        }

        /// <summary>
        /// Generates the WixFile table based on a path to an admin image msi and an Output.
        /// </summary>
        /// <param name="databaseFile">The path to the msi database file in an admin image.</param>
        /// <param name="output">The Output that represents the msi database.</param>
        private void GenerateWixFileTable(string databaseFile, WindowsInstallerData output)
        {
            throw new NotImplementedException();
#if TODO_FIX_UNBINDING_FILES
            var adminRootPath = Path.GetDirectoryName(databaseFile);

            var componentDirectoryIndex = new Hashtable();
            var componentTable = output.Tables["Component"];
            foreach (var row in componentTable.Rows)
            {
                componentDirectoryIndex.Add(row[0], row[2]);
            }

            // Index full source paths for all directories
            var directoryDirectoryParentIndex = new Hashtable();
            var directoryFullPathIndex = new Hashtable();
            var directorySourceNameIndex = new Hashtable();
            var directoryTable = output.Tables["Directory"];
            foreach (var row in directoryTable.Rows)
            {
                directoryDirectoryParentIndex.Add(row[0], row[1]);
                if (null == row[1])
                {
                    directoryFullPathIndex.Add(row[0], adminRootPath);
                }
                else
                {
                    directorySourceNameIndex.Add(row[0], GetAdminSourceName((string)row[2]));
                }
            }

            foreach (DictionaryEntry directoryEntry in directoryDirectoryParentIndex)
            {
                if (!directoryFullPathIndex.ContainsKey(directoryEntry.Key))
                {
                    this.GetAdminFullPath((string)directoryEntry.Key, directoryDirectoryParentIndex, directorySourceNameIndex, directoryFullPathIndex);
                }
            }

            var fileTable = output.Tables["File"];
            var wixFileTable = output.EnsureTable(this.TableDefinitions["WixFile"]);
            foreach (var row in fileTable.Rows)
            {
                var wixFileRow = new WixFileRow(null, this.TableDefinitions["WixFile"]);
                wixFileRow.File = (string)row[0];
                wixFileRow.Directory = (string)componentDirectoryIndex[(string)row[1]];
                wixFileRow.Source = Path.Combine((string)directoryFullPathIndex[wixFileRow.Directory], GetAdminSourceName((string)row[2]));

                if (!File.Exists(wixFileRow.Source))
                {
                    throw new WixException(ErrorMessages.WixFileNotFound(wixFileRow.Source));
                }

                wixFileTable.Rows.Add(wixFileRow);
            }
#endif
        }

        /// <summary>
        /// Gets the full path of a directory. Populates the full path index with the directory's full path and all of its parent directorie's full paths.
        /// </summary>
        /// <param name="directory">The directory identifier.</param>
        /// <param name="directoryDirectoryParentIndex">The Hashtable containing all the directory to directory parent mapping.</param>
        /// <param name="directorySourceNameIndex">The Hashtable containing all the directory to source name mapping.</param>
        /// <param name="directoryFullPathIndex">The Hashtable containing a mapping between all of the directories and their previously calculated full paths.</param>
        /// <returns>The full path to the directory.</returns>
        private string GetAdminFullPath(string directory, Hashtable directoryDirectoryParentIndex, Hashtable directorySourceNameIndex, Hashtable directoryFullPathIndex)
        {
            var parent = (string)directoryDirectoryParentIndex[directory];
            var sourceName = (string)directorySourceNameIndex[directory];

            string parentFullPath;
            if (directoryFullPathIndex.ContainsKey(parent))
            {
                parentFullPath = (string)directoryFullPathIndex[parent];
            }
            else
            {
                parentFullPath = this.GetAdminFullPath(parent, directoryDirectoryParentIndex, directorySourceNameIndex, directoryFullPathIndex);
            }

            if (null == sourceName)
            {
                sourceName = String.Empty;
            }

            var fullPath = Path.Combine(parentFullPath, sourceName);
            directoryFullPathIndex.Add(directory, fullPath);

            return fullPath;
        }

        /// <summary>
        /// Get the source name in an admin image.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <returns>The source name of the directory in an admin image.</returns>
        private static string GetAdminSourceName(string value)
        {
            string name = null;
            string[] names;
            string shortname = null;
            string shortsourcename = null;
            string sourcename = null;

            names = Common.GetNames(value);

            if (null != names[0] && "." != names[0])
            {
                if (null != names[1])
                {
                    shortname = names[0];
                }
                else
                {
                    name = names[0];
                }
            }

            if (null != names[1])
            {
                name = names[1];
            }

            if (null != names[2])
            {
                if (null != names[3])
                {
                    shortsourcename = names[2];
                }
                else
                {
                    sourcename = names[2];
                }
            }

            if (null != names[3])
            {
                sourcename = names[3];
            }

            if (null != sourcename)
            {
                return sourcename;
            }
            else if (null != shortsourcename)
            {
                return shortsourcename;
            }
            else if (null != name)
            {
                return name;
            }
            else
            {
                return shortname;
            }
        }

        /// <summary>
        /// Creates section ids on rows which form logical groupings of resources.
        /// </summary>
        /// <param name="output">The Output that represents the msi database.</param>
        private void GenerateSectionIds(WindowsInstallerData output)
        {
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
                case "WixFile":
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
                        var lockObject = (string)row[0];
                        var tableName = (string)row[1];
                        switch (tableName)
                        {
                        case "File":
                            row.SectionId = (string)fileSectionIdIndex[lockObject];
                            break;
                        case "Registry":
                            row.SectionId = (string)registrySectionIdIndex[lockObject];
                            break;
                        case "ServiceInstall":
                            row.SectionId = (string)serviceInstallSectionIdIndex[lockObject];
                            break;
                        }
                    }
                    break;
                }
            }

            // Now pass the output to each unbinder extension to allow them to analyze the output and determine thier proper section ids.
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
        /// <returns>A Hashtable containing the tables key for each row paired with its assigned section id.</returns>
        private Hashtable AssignSectionIdsToTable(Table table, int rowPrimaryKeyIndex)
        {
            var hashtable = new Hashtable();
            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    row.SectionId = this.GetNewSectionId();
                    hashtable.Add(row[rowPrimaryKeyIndex], row.SectionId);
                }
            }
            return hashtable;
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        private static void ConnectTableToSection(Table table, Hashtable sectionIdIndex, int rowIndex)
        {
            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    if (sectionIdIndex.ContainsKey(row[rowIndex]))
                    {
                        row.SectionId = (string)sectionIdIndex[row[rowIndex]];
                    }
                }
            }
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table and produces an index for other tables to connect to it.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A Hashtable containing the tables key for each row paired with its assigned section id.</returns>
        private static Hashtable ConnectTableToSectionAndIndex(Table table, Hashtable sectionIdIndex, int rowIndex, int rowPrimaryKeyIndex)
        {
            var newHashTable = new Hashtable();
            if (null != table)
            {
                foreach (var row in table.Rows)
                {
                    if (!sectionIdIndex.ContainsKey(row[rowIndex]))
                    {
                        continue;
                    }

                    row.SectionId = (string)sectionIdIndex[row[rowIndex]];
                    if (null != row[rowPrimaryKeyIndex])
                    {
                        newHashTable.Add(row[rowPrimaryKeyIndex], row.SectionId);
                    }
                }
            }
            return newHashTable;
        }

        /// <summary>
        /// Creates a new section identifier to be used when adding a section to an output.
        /// </summary>
        /// <returns>A string representing a new section id.</returns>
        private string GetNewSectionId()
        {
            this.SectionCount++;
            return "wix.section." + this.SectionCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}
