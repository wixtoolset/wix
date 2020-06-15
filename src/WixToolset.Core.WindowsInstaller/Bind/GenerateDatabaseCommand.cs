// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class GenerateDatabaseCommand
    {
        public GenerateDatabaseCommand(IMessaging messaging, IBackendHelper backendHelper, FileSystemManager fileSystemManager, WindowsInstallerData data, string outputPath, TableDefinitionCollection tableDefinitions, string intermediateFolder, int codepage, bool keepAddedColumns, bool suppressAddingValidationRows, bool useSubdirectory)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystemManager = fileSystemManager;
            this.Data = data;
            this.OutputPath = outputPath;
            this.TableDefinitions = tableDefinitions;
            this.IntermediateFolder = intermediateFolder;
            this.Codepage = codepage;
            this.KeepAddedColumns = keepAddedColumns;
            this.SuppressAddingValidationRows = suppressAddingValidationRows;
            this.UseSubDirectory = useSubdirectory;
        }

        private int Codepage { get; }

        private IBackendHelper BackendHelper { get; }

        private FileSystemManager FileSystemManager { get; }

        /// <summary>
        /// Whether to keep columns added in a transform.
        /// </summary>
        private bool KeepAddedColumns { get; }

        private IMessaging Messaging { get; }

        private WindowsInstallerData Data { get; }

        private string OutputPath { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private string IntermediateFolder { get; }

        public List<ITrackedFile> GeneratedTemporaryFiles { get; } = new List<ITrackedFile>();

        /// <summary>
        /// Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.
        /// </summary>
        private bool SuppressAddingValidationRows { get; }

        private bool UseSubDirectory { get; }

        public void Execute()
        {
            // Add the _Validation rows.
            if (!this.SuppressAddingValidationRows)
            {
                this.AddValidationRows();
            }

            var baseDirectory = this.IntermediateFolder;

            if (this.UseSubDirectory)
            {
                var filename = Path.GetFileNameWithoutExtension(this.OutputPath);
                baseDirectory = Path.Combine(baseDirectory, filename);
            }

            var idtFolder = Path.Combine(baseDirectory, "_idts");

            var type = OpenDatabase.CreateDirect;

            if (OutputType.Patch == this.Data.Type)
            {
                type |= OpenDatabase.OpenPatchFile;
            }

            // Localize the codepage if a value was specified directly.
            if (-1 != this.Codepage)
            {
                this.Data.Codepage = this.Codepage;
            }

            try
            {
#if DEBUG
                Console.WriteLine("Opening database at: {0}", this.OutputPath);
#endif

                Directory.CreateDirectory(Path.GetDirectoryName(this.OutputPath));

                Directory.CreateDirectory(idtFolder);

                using (var db = new Database(this.OutputPath, type))
                {
                    // If we're not using the default codepage, import a new one into our
                    // database before we add any tables (or the tables would be added
                    // with the wrong codepage).
                    if (0 != this.Data.Codepage)
                    {
                        this.SetDatabaseCodepage(db, this.Data.Codepage, idtFolder);
                    }

                    this.ImportTables(db, idtFolder);

                    // Insert substorages (usually transforms inside a patch or instance transforms in a package).
                    this.ImportSubStorages(db);

                    // We're good, commit the changes to the new database.
                    db.Commit();
                }
            }
            catch (IOException e)
            {
                // TODO: this error message doesn't seem specific enough
                throw new WixException(ErrorMessages.FileNotFound(new SourceLineNumber(this.OutputPath), this.OutputPath), e);
            }
        }

        private void AddValidationRows()
        {
            var validationTable = this.Data.EnsureTable(this.TableDefinitions["_Validation"]);

            // Add the validation rows for real tables and columns.
            foreach (var table in this.Data.Tables.Where(t => !t.Definition.Unreal))
            {
                foreach (var columnDef in table.Definition.Columns.Where(c => !c.Unreal))
                {
                    var row = validationTable.CreateRow(null);

                    row[0] = table.Name;

                    row[1] = columnDef.Name;

                    if (columnDef.Nullable)
                    {
                        row[2] = "Y";
                    }
                    else
                    {
                        row[2] = "N";
                    }

                    if (columnDef.MinValue.HasValue)
                    {
                        row[3] = columnDef.MinValue.Value;
                    }

                    if (columnDef.MaxValue.HasValue)
                    {
                        row[4] = columnDef.MaxValue.Value;
                    }

                    row[5] = columnDef.KeyTable;

                    if (columnDef.KeyColumn.HasValue)
                    {
                        row[6] = columnDef.KeyColumn.Value;
                    }

                    if (ColumnCategory.Unknown != columnDef.Category)
                    {
                        row[7] = columnDef.Category.ToString();
                    }

                    row[8] = columnDef.Possibilities;

                    row[9] = columnDef.Description;
                }
            }
        }

        private void ImportTables(Database db, string idtDirectory)
        {
            foreach (var table in this.Data.Tables)
            {
                var importTable = table;
                var hasBinaryColumn = false;

                // Skip all unreal tables other than _Streams.
                if (table.Definition.Unreal && "_Streams" != table.Name)
                {
                    continue;
                }

                // Do not put the _Validation table in patches, it is not needed.
                if (OutputType.Patch == this.Data.Type && "_Validation" == table.Name)
                {
                    continue;
                }

                // The only way to import binary data is to copy it to a local subdirectory first.
                // To avoid this extra copying and perf hit, import an empty table with the same
                // definition and later import the binary data from source using records.
                foreach (var columnDefinition in table.Definition.Columns)
                {
                    if (ColumnType.Object == columnDefinition.Type)
                    {
                        importTable = new Table(table.Definition);
                        hasBinaryColumn = true;
                        break;
                    }
                }

                // Create the table via IDT import.
                if ("_Streams" != importTable.Name)
                {
                    try
                    {
                        var command = new CreateIdtFileCommand(this.Messaging, importTable, this.Data.Codepage, idtDirectory, this.KeepAddedColumns);
                        command.Execute();

                        var buildOutput = this.BackendHelper.TrackFile(command.IdtPath, TrackedFileType.Temporary);
                        this.GeneratedTemporaryFiles.Add(buildOutput);

                        db.Import(command.IdtPath);
                    }
                    catch (WixInvalidIdtException)
                    {
                        // If ValidateRows finds anything it doesn't like, it throws
                        importTable.ValidateRows();

                        // Otherwise we rethrow the InvalidIdt
                        throw;
                    }
                }

                // insert the rows via SQL query if this table contains object fields
                if (hasBinaryColumn)
                {
                    var query = new StringBuilder("SELECT ");

                    // Build the query for the view.
                    var firstColumn = true;
                    foreach (var columnDefinition in table.Definition.Columns)
                    {
                        if (columnDefinition.Unreal)
                        {
                            continue;
                        }

                        if (!firstColumn)
                        {
                            query.Append(",");
                        }

                        query.AppendFormat(" `{0}`", columnDefinition.Name);
                        firstColumn = false;
                    }
                    query.AppendFormat(" FROM `{0}`", table.Name);

                    using (var tableView = db.OpenExecuteView(query.ToString()))
                    {
                        // Import each row containing a stream
                        foreach (var row in table.Rows)
                        {
                            using (var record = new Record(table.Definition.Columns.Length))
                            {
                                // Stream names are created by concatenating the name of the table with the values
                                // of the primary key (delimited by periods).
                                var streamName = new StringBuilder();

                                // the _Streams table doesn't prepend the table name (or a period)
                                if ("_Streams" != table.Name)
                                {
                                    streamName.Append(table.Name);
                                }

                                var needStream = false;

                                for (var i = 0; i < table.Definition.Columns.Length; i++)
                                {
                                    var columnDefinition = table.Definition.Columns[i];

                                    if (columnDefinition.Unreal)
                                    {
                                        continue;
                                    }

                                    switch (columnDefinition.Type)
                                    {
                                        case ColumnType.Localized:
                                        case ColumnType.Preserved:
                                        case ColumnType.String:
                                            var str = row.FieldAsString(i);

                                            if (columnDefinition.PrimaryKey)
                                            {
                                                if (0 < streamName.Length)
                                                {
                                                    streamName.Append(".");
                                                }

                                                streamName.Append(str);
                                            }

                                            record.SetString(i + 1, str);
                                            break;
                                        case ColumnType.Number:
                                            record.SetInteger(i + 1, row.FieldAsInteger(i));
                                            break;

                                        case ColumnType.Object:
                                            var path = row.FieldAsString(i);
                                            if (null != path)
                                            {
                                                needStream = true;
                                                try
                                                {
                                                    record.SetStream(i + 1, path);
                                                }
                                                catch (Win32Exception e)
                                                {
                                                    if (0xA1 == e.NativeErrorCode) // ERROR_BAD_PATHNAME
                                                    {
                                                        throw new WixException(ErrorMessages.FileNotFound(row.SourceLineNumbers, path));
                                                    }
                                                    else
                                                    {
                                                        throw new WixException(ErrorMessages.Win32Exception(e.NativeErrorCode, e.Message));
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }

                                // check for a stream name that is more than 62 characters long (the maximum allowed length)
                                if (needStream && MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                {
                                    this.Messaging.Write(ErrorMessages.StreamNameTooLong(row.SourceLineNumbers, table.Name, streamName.ToString(), streamName.Length));
                                }
                                else // add the row to the database
                                {
                                    tableView.Modify(ModifyView.Assign, record);
                                }
                            }
                        }
                    }

                    // Remove rows from the _Streams table for wixpdbs.
                    if ("_Streams" == table.Name)
                    {
                        table.Rows.Clear();
                    }
                }
            }
        }

        private void ImportSubStorages(Database db)
        {
            if (0 < this.Data.SubStorages.Count)
            {
                using (var storagesView = new View(db, "SELECT `Name`, `Data` FROM `_Storages`"))
                {
                    foreach (var subStorage in this.Data.SubStorages)
                    {
                        var transformFile = Path.Combine(this.IntermediateFolder, String.Concat(subStorage.Name, ".mst"));

                        // Bind the transform.
                        var command = new BindTransformCommand(this.Messaging, this.BackendHelper, this.FileSystemManager, this.IntermediateFolder, subStorage.Data, transformFile, this.TableDefinitions);
                        command.Execute();

                        if (this.Messaging.EncounteredError)
                        {
                            continue;
                        }

                        // Add the storage to the database.
                        using (var record = new Record(2))
                        {
                            record.SetString(1, subStorage.Name);
                            record.SetStream(2, transformFile);
                            storagesView.Modify(ModifyView.Assign, record);
                        }
                    }
                }
            }
        }

        private void SetDatabaseCodepage(Database db, int codepage, string idtFolder)
        {
            // Write out the _ForceCodepage IDT file.
            var idtPath = Path.Combine(idtFolder, "_ForceCodepage.idt");
            using (var idtFile = new StreamWriter(idtPath, false, Encoding.ASCII))
            {
                idtFile.WriteLine(); // dummy column name record
                idtFile.WriteLine(); // dummy column definition record
                idtFile.Write(codepage);
                idtFile.WriteLine("\t_ForceCodepage");
            }

            var trackId = this.BackendHelper.TrackFile(idtPath, TrackedFileType.Temporary);
            this.GeneratedTemporaryFiles.Add(trackId);

            // Try to import the table into the MSI.
            try
            {
                db.Import(idtPath);
            }
            catch (WixInvalidIdtException)
            {
                // The IDT should be valid, so an invalid code page was given.
                throw new WixException(ErrorMessages.IllegalCodepage(codepage));
            }
        }
    }
}
