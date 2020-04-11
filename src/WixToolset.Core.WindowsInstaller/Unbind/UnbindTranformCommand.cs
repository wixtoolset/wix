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
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class UnbindTransformCommand
    {
        public UnbindTransformCommand(IMessaging messaging, string transformFile, string exportBasePath, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.TransformFile = transformFile;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;

            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
        }

        private IMessaging Messaging { get; }

        private string TransformFile { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private string EmptyFile { get; set; }
        
        public WindowsInstallerData Execute()
        {
            var transform = new WindowsInstallerData(new SourceLineNumber(this.TransformFile));
            transform.Type = OutputType.Transform;

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
            var schemaOutput = new WindowsInstallerData(null);
            var msiDatabaseFile = Path.Combine(this.IntermediateFolder, "schema.msi");
            foreach (var tableDefinition in this.TableDefinitions)
            {
                // skip unreal tables and the Patch table
                if (!tableDefinition.Unreal && "Patch" != tableDefinition.Name)
                {
                    schemaOutput.EnsureTable(tableDefinition);
                }
            }

            var addedRows = new Dictionary<string, Row>();
            Table transformViewTable;

            // Bind the schema msi.
            this.GenerateDatabase(schemaOutput, msiDatabaseFile);

            // apply the transform to the database and retrieve the modifications
            using (var msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
            {
                // apply the transform with the ViewTransform option to collect all the modifications
                msiDatabase.ApplyTransform(this.TransformFile, TransformErrorConditions.All | TransformErrorConditions.ViewTransform);

                // unbind the database
                var unbindCommand = new UnbindDatabaseCommand(this.Messaging, msiDatabase, msiDatabaseFile, OutputType.Product, this.ExportBasePath, this.IntermediateFolder, false, false, skipSummaryInfo: true);
                var transformViewOutput = unbindCommand.Execute();

                // index the added and possibly modified rows (added rows may also appears as modified rows)
                transformViewTable = transformViewOutput.Tables["_TransformView"];
                var modifiedRows = new Hashtable();
                foreach (var row in transformViewTable.Rows)
                {
                    var tableName = (string)row[0];
                    var columnName = (string)row[1];
                    var primaryKeys = (string)row[2];

                    if ("INSERT" == columnName)
                    {
                        var index = String.Concat(tableName, ':', primaryKeys);

                        addedRows.Add(index, null);
                    }
                    else if ("CREATE" != columnName && "DELETE" != columnName && "DROP" != columnName && null != primaryKeys) // modified row
                    {
                        var index = String.Concat(tableName, ':', primaryKeys);

                        modifiedRows[index] = row;
                    }
                }

                // create placeholder rows for modified rows to make the transform insert the updated values when its applied
                foreach (Row row in modifiedRows.Values)
                {
                    var tableName = (string)row[0];
                    var columnName = (string)row[1];
                    var primaryKeys = (string)row[2];

                    var index = String.Concat(tableName, ':', primaryKeys);

                    // ignore information for added rows
                    if (!addedRows.ContainsKey(index))
                    {
                        var table = schemaOutput.Tables[tableName];
                        this.CreateRow(table, primaryKeys, true);
                    }
                }
            }

            // Re-bind the schema output with the placeholder rows.
            this.GenerateDatabase(schemaOutput, msiDatabaseFile);

            // apply the transform to the database and retrieve the modifications
            using (var msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
            {
                try
                {
                    // apply the transform
                    msiDatabase.ApplyTransform(this.TransformFile, TransformErrorConditions.All);

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

                // unbind the database
                var unbindCommand = new UnbindDatabaseCommand(this.Messaging, msiDatabase, msiDatabaseFile, OutputType.Product, this.ExportBasePath, this.IntermediateFolder, false, false, skipSummaryInfo: true);
                var output = unbindCommand.Execute();

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
                    var tableName = (string)row[0];
                    var columnName = (string)row[1];
                    var primaryKeys = (string)row[2];

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

            return transform;
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
                            using (var fileStream = File.Create(this.EmptyFile))
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

        private void GenerateDatabase(WindowsInstallerData output, string databaseFile)
        {
            var command = new GenerateDatabaseCommand(this.Messaging, null, null, output, databaseFile, this.TableDefinitions, this.IntermediateFolder, codepage: -1, keepAddedColumns: true, suppressAddingValidationRows: true, useSubdirectory: false);
            command.Execute();
        }
    }
}
