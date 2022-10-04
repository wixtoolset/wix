// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class UnbindTransformCommand
    {
        public UnbindTransformCommand(IMessaging messaging, IBackendHelper backendHelper, IFileSystem fileSystem, IPathResolver pathResolver, FileSystemManager fileSystemManager, string transformFile, string exportBasePath, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystem = fileSystem;
            this.PathResolver = pathResolver;
            this.FileSystemManager = fileSystemManager;
            this.TransformFile = transformFile;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;

            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IFileSystem FileSystem { get; }

        private IPathResolver PathResolver { get; }

        private FileSystemManager FileSystemManager { get; }

        private string TransformFile { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private string EmptyFile { get; set; }

        public WindowsInstallerData Execute()
        {
            var transform = new WindowsInstallerData(new SourceLineNumber(this.TransformFile))
            {
                Type = OutputType.Transform
            };

            // get the summary information table
            using (var summaryInformation = new SummaryInformation(this.TransformFile))
            {
                var table = transform.EnsureTable(this.TableDefinitions["_SummaryInformation"]);

                for (var i = 1; 19 >= i; i++)
                {
                    var value = summaryInformation.GetProperty(i);

                    if (0 < value.Length)
                    {
                        var row = table.CreateRow(transform.SourceLineNumbers);
                        row[0] = i;
                        row[1] = value;
                    }
                }
            }

            // create a schema msi which hopefully matches the table schemas in the transform
            var schemaDatabasePath = Path.Combine(this.IntermediateFolder, "schema.msi");
            var schemaData = this.CreateSchemaData(schemaDatabasePath);

            // Bind the schema msi.
            this.GenerateDatabase(schemaData);

            var transformViewTable = this.OpenTransformViewForAddedAndModifiedRows(schemaDatabasePath);

            var addedRows = this.CreatePlaceholdersForModifiedRowsAndIndexAddedRows(schemaData, transformViewTable);

            // Re-bind the schema output with the placeholder rows over top the original schema database.
            this.GenerateDatabase(schemaData);

            this.PopulateTransformFromView(schemaDatabasePath, transform, transformViewTable, addedRows);

            return transform;
        }

        private WindowsInstallerData CreateSchemaData(string schemaDatabasePath)
        {
            var schemaData = new WindowsInstallerData(new SourceLineNumber(schemaDatabasePath))
            {
                Type = OutputType.Product,
            };

            foreach (var tableDefinition in this.TableDefinitions)
            {
                // skip unreal tables and the Patch table
                if (!tableDefinition.Unreal && "Patch" != tableDefinition.Name)
                {
                    schemaData.EnsureTable(tableDefinition);
                }
            }

            return schemaData;
        }

        private Table OpenTransformViewForAddedAndModifiedRows(string schemaDatabasePath)
        {
            // Apply the transform with the ViewTransform option to collect all the modifications.
            using (var msiDatabase = this.ApplyTransformToSchemaDatabase(schemaDatabasePath, TransformErrorConditions.All | TransformErrorConditions.ViewTransform))
            {
                // unbind the database
                var unbindCommand = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystem, this.PathResolver, schemaDatabasePath, msiDatabase, OutputType.Product, null, null, this.IntermediateFolder, enableDemodularization: false, skipSummaryInfo: true);
                var transformViewOutput = unbindCommand.Execute();

                return transformViewOutput.Tables["_TransformView"];
            }
        }

        private Dictionary<string, Row> CreatePlaceholdersForModifiedRowsAndIndexAddedRows(WindowsInstallerData schemaData, Table transformViewTable)
        {
            // Index the added and possibly modified rows (added rows may also appears as modified rows).
            var addedRows = new Dictionary<string, Row>();
            var modifiedRows = new Dictionary<string, TableNameWithPrimaryKeys>();

            foreach (var row in transformViewTable.Rows)
            {
                var tableName = row.FieldAsString(0);
                var columnName = row.FieldAsString(1);
                var primaryKeys = row.FieldAsString(2);

                if ("INSERT" == columnName)
                {
                    var index = String.Concat(tableName, ':', primaryKeys);

                    addedRows.Add(index, null);
                }
                else if ("CREATE" != columnName && "DELETE" != columnName && "DROP" != columnName && null != primaryKeys) // modified row
                {
                    var index = String.Concat(tableName, ':', primaryKeys);

                    if (!modifiedRows.ContainsKey(index))
                    {
                        modifiedRows.Add(index, new TableNameWithPrimaryKeys { TableName = tableName, PrimaryKeys = primaryKeys });
                    }
                }
            }

            // Create placeholder rows for modified rows to make the transform insert the updated values when its applied.
            foreach (var kvp in modifiedRows)
            {
                var index = kvp.Key;
                var tableNameWithPrimaryKey = kvp.Value;

                // Ignore added rows.
                if (!addedRows.ContainsKey(index))
                {
                    var table = schemaData.Tables[tableNameWithPrimaryKey.TableName];
                    this.CreateRow(table, tableNameWithPrimaryKey.PrimaryKeys, setRequiredFields: true);
                }
            }

            return addedRows;
        }

        private void PopulateTransformFromView(string schemaDatabasePath, WindowsInstallerData transform, Table transformViewTable, Dictionary<string, Row> addedRows)
        {
            WindowsInstallerData output;
            // Apply the transform to the database and retrieve the modifications
            using (var database = this.ApplyTransformToSchemaDatabase(schemaDatabasePath, TransformErrorConditions.All))
            {

                // unbind the database
                var unbindCommand = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystem, this.PathResolver, schemaDatabasePath, database, OutputType.Product, this.ExportBasePath, null, this.IntermediateFolder, enableDemodularization: false, skipSummaryInfo: true);
                output = unbindCommand.Execute();
            }

            // index all the rows to easily find modified rows
            var rows = new Dictionary<string, Row>();
            foreach (var table in output.Tables)
            {
                foreach (var row in table.Rows)
                {
                    rows.Add(String.Concat(table.Name, ':', row.GetPrimaryKey('\t', " ")), row);
                }
            }

            // process the _TransformView rows into transform rows
            foreach (var row in transformViewTable.Rows)
            {
                var tableName = row.FieldAsString(0);
                var columnName = row.FieldAsString(1);
                var primaryKeys = row.FieldAsString(2);

                var table = transform.EnsureTable(this.TableDefinitions[tableName]);

                if ("CREATE" == columnName) // added table
                {
                    table.Operation = TableOperation.Add;
                }
                else if ("DELETE" == columnName) // deleted row
                {
                    var deletedRow = this.CreateRow(table, primaryKeys, false);
                    deletedRow.Operation = RowOperation.Delete;
                }
                else if ("DROP" == columnName) // dropped table
                {
                    table.Operation = TableOperation.Drop;
                }
                else if ("INSERT" == columnName) // added row
                {
                    var index = String.Concat(tableName, ':', primaryKeys);
                    var addedRow = rows[index];
                    addedRow.Operation = RowOperation.Add;
                    table.Rows.Add(addedRow);
                }
                else if (null != primaryKeys) // modified row
                {
                    var index = String.Concat(tableName, ':', primaryKeys);

                    // the _TransformView table includes information for added rows
                    // that looks like modified rows so it sometimes needs to be ignored
                    if (!addedRows.ContainsKey(index))
                    {
                        var modifiedRow = rows[index];

                        // mark the field as modified
                        var indexOfModifiedValue = -1;
                        for (var i = 0; i < modifiedRow.TableDefinition.Columns.Length; ++i)
                        {
                            if (columnName.Equals(modifiedRow.TableDefinition.Columns[i].Name, StringComparison.Ordinal))
                            {
                                indexOfModifiedValue = i;
                                break;
                            }
                        }
                        modifiedRow.Fields[indexOfModifiedValue].Modified = true;

                        // move the modified row into the transform the first time its encountered
                        if (RowOperation.None == modifiedRow.Operation)
                        {
                            modifiedRow.Operation = RowOperation.Modify;
                            table.Rows.Add(modifiedRow);
                        }
                    }
                }
                else // added column
                {
                    var column = table.Definition.Columns.Single(c => c.Name.Equals(columnName, StringComparison.Ordinal));
                    column.Added = true;
                }
            }
        }

        private Database ApplyTransformToSchemaDatabase(string schemaDatabasePath, TransformErrorConditions transformConditions)
        {
            var msiDatabase = new Database(schemaDatabasePath, OpenDatabase.Transact);

            try
            {
                // apply the transform
                msiDatabase.ApplyTransform(this.TransformFile, transformConditions);

                // commit the database to guard against weird errors with streams
                msiDatabase.Commit();
            }
            catch (Win32Exception ex)
            {
                if (0x65B == ex.NativeErrorCode)
                {
                    // this commonly happens when the transform was built
                    // against a database schema different from the internal
                    // table definitions
                    throw new WixException(ErrorMessages.TransformSchemaMismatch());
                }
            }

            return msiDatabase;
        }

        /// <summary>
        /// Create a deleted or modified row.
        /// </summary>
        /// <param name="table">The table containing the row.</param>
        /// <param name="primaryKeys">The primary keys of the row.</param>
        /// <param name="setRequiredFields">Option to set all required fields with placeholder values.</param>
        /// <returns>The new row.</returns>
        private Row CreateRow(Table table, string primaryKeys, bool setRequiredFields)
        {
            var row = table.CreateRow(null);

            var primaryKeyParts = primaryKeys.Split('\t');
            var primaryKeyPartIndex = 0;

            for (var i = 0; i < table.Definition.Columns.Length; i++)
            {
                var columnDefinition = table.Definition.Columns[i];

                if (columnDefinition.PrimaryKey)
                {
                    if (ColumnType.Number == columnDefinition.Type && !columnDefinition.IsLocalizable)
                    {
                        row[i] = Convert.ToInt32(primaryKeyParts[primaryKeyPartIndex++], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        row[i] = primaryKeyParts[primaryKeyPartIndex++];
                    }
                }
                else if (setRequiredFields)
                {
                    if (ColumnType.Number == columnDefinition.Type && !columnDefinition.IsLocalizable)
                    {
                        row[i] = 1;
                    }
                    else if (ColumnType.Object == columnDefinition.Type)
                    {
                        if (null == this.EmptyFile)
                        {
                            this.EmptyFile = Path.Combine(this.IntermediateFolder, ".empty");
                            using (var fileStream = this.FileSystem.OpenFile(this.EmptyFile, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                            }
                        }

                        row[i] = this.EmptyFile;
                    }
                    else
                    {
                        row[i] = "1";
                    }
                }
            }

            return row;
        }

        private void GenerateDatabase(WindowsInstallerData data)
        {
            var command = new GenerateDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystem, this.FileSystemManager, data, data.SourceLineNumbers.FileName, this.TableDefinitions, this.IntermediateFolder, keepAddedColumns: true, suppressAddingValidationRows: true, useSubdirectory: false);
            command.Execute();
        }

        private class TableNameWithPrimaryKeys
        {
            public string TableName { get; set; }

            public string PrimaryKeys { get; set; }
        }
    }
}
