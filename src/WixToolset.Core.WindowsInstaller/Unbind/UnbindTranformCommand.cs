// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class UnbindTransformCommand
    {
        public UnbindTransformCommand(IMessaging messaging, string transformFile, string exportBasePath, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.TransformFile = transformFile;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;

            this.TableDefinitions = WindowsInstallerStandardInternal.GetTableDefinitions();
        }

        private IMessaging Messaging { get; }

        private string TransformFile { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private string EmptyFile { get; set; }
        
        public WindowsInstallerData Execute()
        {
            WindowsInstallerData transform = new WindowsInstallerData(new SourceLineNumber(this.TransformFile));
            transform.Type = OutputType.Transform;

            // get the summary information table
            using (SummaryInformation summaryInformation = new SummaryInformation(this.TransformFile))
            {
                Table table = transform.EnsureTable(this.TableDefinitions["_SummaryInformation"]);

                for (int i = 1; 19 >= i; i++)
                {
                    string value = summaryInformation.GetProperty(i);

                    if (0 < value.Length)
                    {
                        Row row = table.CreateRow(transform.SourceLineNumbers);
                        row[0] = i;
                        row[1] = value;
                    }
                }
            }

            // create a schema msi which hopefully matches the table schemas in the transform
            WindowsInstallerData schemaOutput = new WindowsInstallerData(null);
            string msiDatabaseFile = Path.Combine(this.IntermediateFolder, "schema.msi");
            foreach (TableDefinition tableDefinition in this.TableDefinitions)
            {
                // skip unreal tables and the Patch table
                if (!tableDefinition.Unreal && "Patch" != tableDefinition.Name)
                {
                    schemaOutput.EnsureTable(tableDefinition);
                }
            }

            Hashtable addedRows = new Hashtable();
            Table transformViewTable;

            // Bind the schema msi.
            this.GenerateDatabase(schemaOutput, msiDatabaseFile);

            // apply the transform to the database and retrieve the modifications
            using (Database msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
            {
                // apply the transform with the ViewTransform option to collect all the modifications
                msiDatabase.ApplyTransform(this.TransformFile, TransformErrorConditions.All | TransformErrorConditions.ViewTransform);

                // unbind the database
                var unbindCommand = new UnbindDatabaseCommand(this.Messaging, msiDatabase, msiDatabaseFile, OutputType.Product, this.ExportBasePath, this.IntermediateFolder, false, false, skipSummaryInfo: true);
                WindowsInstallerData transformViewOutput = unbindCommand.Execute();

                // index the added and possibly modified rows (added rows may also appears as modified rows)
                transformViewTable = transformViewOutput.Tables["_TransformView"];
                Hashtable modifiedRows = new Hashtable();
                foreach (Row row in transformViewTable.Rows)
                {
                    string tableName = (string)row[0];
                    string columnName = (string)row[1];
                    string primaryKeys = (string)row[2];

                    if ("INSERT" == columnName)
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);

                        addedRows.Add(index, null);
                    }
                    else if ("CREATE" != columnName && "DELETE" != columnName && "DROP" != columnName && null != primaryKeys) // modified row
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);

                        modifiedRows[index] = row;
                    }
                }

                // create placeholder rows for modified rows to make the transform insert the updated values when its applied
                foreach (Row row in modifiedRows.Values)
                {
                    string tableName = (string)row[0];
                    string columnName = (string)row[1];
                    string primaryKeys = (string)row[2];

                    string index = String.Concat(tableName, ':', primaryKeys);

                    // ignore information for added rows
                    if (!addedRows.Contains(index))
                    {
                        Table table = schemaOutput.Tables[tableName];
                        this.CreateRow(table, primaryKeys, true);
                    }
                }
            }

            // Re-bind the schema output with the placeholder rows.
            this.GenerateDatabase(schemaOutput, msiDatabaseFile);

            // apply the transform to the database and retrieve the modifications
            using (Database msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
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
                WindowsInstallerData output = unbindCommand.Execute();

                // index all the rows to easily find modified rows
                Hashtable rows = new Hashtable();
                foreach (Table table in output.Tables)
                {
                    foreach (Row row in table.Rows)
                    {
                        rows.Add(String.Concat(table.Name, ':', row.GetPrimaryKey('\t', " ")), row);
                    }
                }

                // process the _TransformView rows into transform rows
                foreach (Row row in transformViewTable.Rows)
                {
                    string tableName = (string)row[0];
                    string columnName = (string)row[1];
                    string primaryKeys = (string)row[2];

                    Table table = transform.EnsureTable(this.TableDefinitions[tableName]);

                    if ("CREATE" == columnName) // added table
                    {
                        table.Operation = TableOperation.Add;
                    }
                    else if ("DELETE" == columnName) // deleted row
                    {
                        Row deletedRow = this.CreateRow(table, primaryKeys, false);
                        deletedRow.Operation = RowOperation.Delete;
                    }
                    else if ("DROP" == columnName) // dropped table
                    {
                        table.Operation = TableOperation.Drop;
                    }
                    else if ("INSERT" == columnName) // added row
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);
                        Row addedRow = (Row)rows[index];
                        addedRow.Operation = RowOperation.Add;
                        table.Rows.Add(addedRow);
                    }
                    else if (null != primaryKeys) // modified row
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);

                        // the _TransformView table includes information for added rows
                        // that looks like modified rows so it sometimes needs to be ignored
                        if (!addedRows.Contains(index))
                        {
                            Row modifiedRow = (Row)rows[index];

                            // mark the field as modified
                            int indexOfModifiedValue = -1;
                            for (int i = 0; i < modifiedRow.TableDefinition.Columns.Length; ++i)
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
                        ColumnDefinition column = table.Definition.Columns.Single(c => c.Name.Equals(columnName, StringComparison.Ordinal));
                        column.Added = true;
                    }
                }
            }

            return transform;
        }

        private void GenerateDatabase(WindowsInstallerData output, string databaseFile)
        {
            var command = new GenerateDatabaseCommand();
            command.Extensions = Array.Empty<IFileSystemExtension>();
            command.Output = output;
            command.OutputPath = databaseFile;
            command.KeepAddedColumns = true;
            command.UseSubDirectory = false;
            command.SuppressAddingValidationRows = true;
            command.TableDefinitions = this.TableDefinitions;
            command.IntermediateFolder = this.IntermediateFolder;
            command.Codepage = -1;
            command.Execute();
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
            Row row = table.CreateRow(null);

            string[] primaryKeyParts = primaryKeys.Split('\t');
            int primaryKeyPartIndex = 0;

            for (int i = 0; i < table.Definition.Columns.Length; i++)
            {
                ColumnDefinition columnDefinition = table.Definition.Columns[i];

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
                            this.EmptyFile = Path.GetTempFileName() + ".empty";
                            using (FileStream fileStream = File.Create(this.EmptyFile))
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
    }
}
