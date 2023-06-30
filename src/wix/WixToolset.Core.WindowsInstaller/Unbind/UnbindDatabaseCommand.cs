// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class UnbindDatabaseCommand
    {
        private static readonly Regex Modularization = new Regex(@"\.[0-9A-Fa-f]{8}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{12}");

        public UnbindDatabaseCommand(IMessaging messaging, IBackendHelper backendHelper, IFileSystem fileSystem, IPathResolver pathResolver, string databasePath, Database database, OutputType outputType, string exportBasePath, string extractFilesFolder, string intermediateFolder, bool enableDemodularization, bool skipSummaryInfo)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystem = fileSystem;
            this.PathResolver = pathResolver;
            this.DatabasePath = databasePath;
            this.Database = database;
            this.OutputType = outputType;
            this.ExportBasePath = exportBasePath;
            this.ExtractFilesFolder = extractFilesFolder;
            this.IntermediateFolder = intermediateFolder;
            this.EnableDemodularization = enableDemodularization;
            this.SkipSummaryInfo = skipSummaryInfo;

            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private  IFileSystem FileSystem { get; }

        private IPathResolver PathResolver { get; }

        private Database Database { get; set; }

        private string DatabasePath { get; }

        private OutputType OutputType { get; }

        private string ExportBasePath { get; }

        private string ExtractFilesFolder { get; }

        private string IntermediateFolder { get; }

        private bool EnableDemodularization { get; }

        private bool SkipSummaryInfo { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        public bool AdminImage { get; private set; }

        public WindowsInstallerData Data { get; private set; }

        public IEnumerable<string> ExportedFiles { get; private set; }

        public WindowsInstallerData Execute()
        {
            var adminImage = false;
            var exportedFiles = new List<string>();

            var data = new WindowsInstallerData(new SourceLineNumber(this.DatabasePath))
            {
                Type = this.OutputType
            };

            Database database = null;
            try
            {
                if (this.Database == null)
                {
                    database = new Database(this.DatabasePath, OpenDatabase.ReadOnly);
                    this.Database = database;
                }

                Directory.CreateDirectory(this.IntermediateFolder);

                data.Codepage = this.GetCodePage();

                var modularizationGuid = this.ProcessTables(data, exportedFiles);

                var summaryInfo = this.ProcessSummaryInfo(data, modularizationGuid);

                this.UpdateUnrealFileColumns(this.DatabasePath, data, summaryInfo, exportedFiles);
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    throw new WixException(ErrorMessages.OpenDatabaseFailed(this.DatabasePath));
                }

                throw;
            }
            finally
            {
                database?.Dispose();
            }

            this.AdminImage = adminImage;
            this.Data = data;
            this.ExportedFiles = exportedFiles;

            return data;
        }

        private int GetCodePage()
        {
            var codepage = 0;

            this.Database.Export("_ForceCodepage", this.IntermediateFolder, "_ForceCodepage.idt");

            var lines = File.ReadAllLines(Path.Combine(this.IntermediateFolder, "_ForceCodepage.idt"));

            if (lines.Length == 3)
            {
                var data = lines[2].Split('\t');

                if (2 == data.Length)
                {
                    codepage = Convert.ToInt32(data[0], CultureInfo.InvariantCulture);
                }
            }

            return codepage;
        }

        private string ProcessTables(WindowsInstallerData output, List<string> exportedFiles)
        {
            View validationView = null;
            string modularizationGuid = null;
            string modularizationSuffix = null;

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
                    foreach (var tableRecord in tablesView.Records)
                    {
                        var tableName = tableRecord.GetString(1);

                        using (var tableView = this.Database.OpenExecuteView($"SELECT * FROM `{tableName}`"))
                        {
                            var tableDefinition = this.GetTableDefinition(tableName, tableView, validationView);
                            var table = new Table(tableDefinition);

                            foreach (var rowRecord in tableView.Records)
                            {
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
                                                var intValue = rowRecord.GetInteger(i + 1);
                                                var success = row.Fields[i].Column.IsLocalizable ? row.BestEffortSetField(i, Convert.ToString(intValue, CultureInfo.InvariantCulture)) : row.BestEffortSetField(i, intValue);

                                                if (!success)
                                                {
                                                    this.Messaging.Write(WarningMessages.BadColumnDataIgnored(row.SourceLineNumbers, Convert.ToString(intValue, CultureInfo.InvariantCulture), tableName, row.Fields[i].Column.Name));
                                                }
                                                break;
                                            case ColumnType.Object:
                                                var source = "FILE NOT EXPORTED";

                                                if (null != this.ExportBasePath)
                                                {
                                                    source = Path.Combine(this.ExportBasePath, tableName, row.GetPrimaryKey('.'));

                                                    if (!String.IsNullOrEmpty(modularizationSuffix))
                                                    {
                                                        source += modularizationSuffix;
                                                    }

                                                    Directory.CreateDirectory(Path.Combine(this.ExportBasePath, tableName));

                                                    using (var fs = this.FileSystem.OpenFile(null, source, FileMode.Create, FileAccess.Write, FileShare.None))
                                                    {
                                                        int bytesRead;
                                                        var buffer = new byte[4096];

                                                        while (0 != (bytesRead = rowRecord.GetStream(i + 1, buffer, buffer.Length)))
                                                        {
                                                            fs.Write(buffer, 0, bytesRead);
                                                        }
                                                    }

                                                    exportedFiles.Add(source);
                                                }

                                                row[i] = source;
                                                break;
                                            default:
                                                var value = rowRecord.GetString(i + 1);

                                                switch (row.Fields[i].Column.Category)
                                                {
                                                    case ColumnCategory.Guid:
                                                        value = value.ToUpperInvariant();
                                                        break;
                                                }

                                                // De-modularize
                                                if (this.EnableDemodularization && OutputType.Module == output.Type && ColumnModularizeType.None != row.Fields[i].Column.ModularizeType)
                                                {
                                                    if (null == modularizationGuid)
                                                    {
                                                        var match = Modularization.Match(value);
                                                        modularizationSuffix = match.Value;

                                                        if (match.Success)
                                                        {
                                                            modularizationGuid = String.Concat('{', match.Value.Substring(1).Replace('_', '-'), '}');
                                                        }
                                                    }

                                                    value = Modularization.Replace(value, String.Empty);
                                                }

#if TODO_MOVE_TO_DECOMPILER
                                                // escape "$(" for the preprocessor
                                                value = value.Replace("$(", "$$(");

                                                // escape things that look like wix variables
                                                // TODO: Evaluate this requirement.
                                                //var matches = Common.WixVariableRegex.Matches(value);
                                                //for (var j = matches.Count - 1; 0 <= j; j--)
                                                //{
                                                //    value = value.Insert(matches[j].Index, "!");
                                                //}
#endif

                                                row[i] = value;
                                                break;
                                        }
                                    }
                                }
                            }

                            output.Tables.Add(table);
                        }
                    }
                }
            }
            finally
            {
                validationView?.Close();
            }

            return modularizationGuid;
        }

        private SummaryInformationBits ProcessSummaryInfo(WindowsInstallerData output, string modularizationGuid)
        {
            var result = new SummaryInformationBits();

            if (!this.SkipSummaryInfo)
            {
                using (var summaryInformation = new SummaryInformation(this.Database))
                {
                    var table = new Table(this.TableDefinitions["_SummaryInformation"]);

                    for (var i = 1; 19 >= i; i++)
                    {
                        var value = summaryInformation.GetProperty(i);

                        // Set the modularization guid as the PackageCode, for merge modules.
                        if (i == (int)SummaryInformation.Package.PackageCode && !String.IsNullOrEmpty(modularizationGuid))
                        {
                            var row = table.CreateRow(output.SourceLineNumbers);
                            row[0] = i;
                            row[1] = modularizationGuid;
                        }
                        else if (0 < value.Length)
                        {
                            var row = table.CreateRow(output.SourceLineNumbers);
                            row[0] = i;
                            row[1] = value;

                            if (i == (int)SummaryInformation.Package.FileAndElevatedFlags)
                            {
                                var wordcount = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                                result.LongFilenames = (wordcount & 0x1) != 0x1;
                                result.Compressed = (wordcount & 0x2) == 0x2;
                                result.AdminImage = (wordcount & 0x4) == 0x4;
                            }
                        }
                    }

                    output.Tables.Add(table);
                }
            }

            return result;
        }

        private TableDefinition GetTableDefinition(string tableName, View tableView, View validationView)
        {
            // Use our table definitions whenever possible since they will be used when compiling the source code anyway.
            // This also allows us to take advantage of WiX concepts like localizable columns which current code assumes.
            if (this.TableDefinitions.Contains(tableName))
            {
                return this.TableDefinitions[tableName];
            }

            ColumnDefinition[] columns;
            using (Record columnNameRecord = tableView.GetColumnNames(),
                          columnTypeRecord = tableView.GetColumnTypes())
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
                    ColumnType columnType;
                    switch (Char.ToLowerInvariant(idtType[0]))
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
                    var length = Convert.ToInt32(idtType.Substring(1), CultureInfo.InvariantCulture);
                    var nullable = Char.IsUpper(idtType[0]);

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
                                    if (!Enum.TryParse(category, true, out columnCategory))
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

            return new TableDefinition(tableName, null, columns, false);
        }

        private void UpdateUnrealFileColumns(string databaseFile, WindowsInstallerData output, SummaryInformationBits summaryInformation, List<string> exportedFiles)
        {
            var fileRows = output.Tables["File"]?.Rows;

            if (fileRows == null || fileRows.Count == 0)
            {
                return;
            }

            this.UpdateFileRowsDiskId(output, fileRows);

            this.UpdateFileRowsSource(databaseFile, output, fileRows, summaryInformation, exportedFiles);
        }

        private void UpdateFileRowsDiskId(WindowsInstallerData output, IList<Row> fileRows)
        {
            var mediaRows = output.Tables["Media"]?.Rows?.Cast<MediaRow>()?.OrderBy(r => r.LastSequence)?.ToList();

            var lastMediaRowIndex = 0;
            var lastMediaRow = (mediaRows == null || mediaRows.Count == 0) ? null : mediaRows[lastMediaRowIndex];

            foreach (var fileRow in fileRows.Cast<FileRow>()?.OrderBy(r => r.Sequence))
            {
                while (lastMediaRow != null && fileRow.Sequence > lastMediaRow.LastSequence)
                {
                    ++lastMediaRowIndex;

                    lastMediaRow = lastMediaRowIndex < mediaRows.Count ? mediaRows[lastMediaRowIndex] : null;
                }

                fileRow.DiskId = lastMediaRow?.DiskId ?? 1;
            }
        }

        private void UpdateFileRowsSource(string databasePath, WindowsInstallerData output, IList<Row> fileRows, SummaryInformationBits summaryInformation, List<string> exportedFiles)
        {
            var databaseFolder = Path.GetDirectoryName(databasePath);

            var componentDirectoryIndex = output.Tables["Component"].Rows.Cast<ComponentRow>().ToDictionary(r => r.Component, r => r.Directory);

            // Index full source paths for all directories
            var directories = new Dictionary<string, IResolvedDirectory>();

            var directoryTable = output.Tables["Directory"];
            foreach (var row in directoryTable.Rows)
            {
                var sourceName = this.BackendHelper.GetMsiFileName(row.FieldAsString(2), source: true, longName: summaryInformation.LongFilenames);
                var resolvedDirectory = this.BackendHelper.CreateResolvedDirectory(row.FieldAsString(1), sourceName);

                directories.Add(row.FieldAsString(0), resolvedDirectory);
            }

            if (summaryInformation.AdminImage)
            {
                foreach (var fileRow in fileRows.Cast<FileRow>())
                {
                    var directoryId = componentDirectoryIndex[fileRow.Component];
                    var relativeFileLayoutPath = this.PathResolver.GetFileSourcePath(directories, directoryId, fileRow.FileName, compressed: false, useLongName: summaryInformation.LongFilenames);

                    fileRow.Source = Path.Combine(databaseFolder, relativeFileLayoutPath);
                }
            }
            else
            {
                var extractedFileIds = new HashSet<string>();

                if (!String.IsNullOrEmpty(this.ExtractFilesFolder))
                {
                    var extractCommand = new ExtractCabinetsCommand(this.FileSystem, output, this.Database, this.DatabasePath, this.ExtractFilesFolder, this.IntermediateFolder);
                    extractCommand.Execute();

                    extractedFileIds = new HashSet<string>(extractCommand.ExtractedFileIdsWithMediaRow.Keys, StringComparer.OrdinalIgnoreCase);
                    exportedFiles.AddRange(extractedFileIds);
                }

                foreach (var fileRow in fileRows.Cast<FileRow>())
                {
                    var source = "FILE NOT EXPORTED";

                    if (fileRow.Compressed == YesNoType.Yes || (fileRow.Compressed == YesNoType.NotSet && summaryInformation.Compressed))
                    {
                        if (extractedFileIds.Contains(fileRow.File))
                        {
                            source = Path.Combine(this.ExtractFilesFolder, fileRow.File);
                        }
                    }
                    else if (componentDirectoryIndex.TryGetValue(fileRow.Component, out var directoryId)) // this can happen when unbinding an invalid MSI file or MST file with select table modifications.
                    {
                        var relativeFileLayoutPath = this.PathResolver.GetFileSourcePath(directories, directoryId, fileRow.FileName, compressed: false, useLongName: summaryInformation.LongFilenames);

                        source = Path.Combine(databaseFolder, relativeFileLayoutPath);
                    }

                    fileRow.Source = source;
                }
            }
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
        private string GetAdminSourceName(string value)
        {
            string name = null;
            string[] names;
            string shortname = null;
            string shortsourcename = null;
            string sourcename = null;

            names = this.BackendHelper.SplitMsiFileName(value);

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

        private class SummaryInformationBits
        {
            public bool AdminImage { get; set; }

            public bool Compressed { get; set; }

            public bool LongFilenames { get; set; }
        }
    }
}
