// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;
    using WixToolset.Extensibility.Data;
    using WixToolset.Core.WindowsInstaller.Msi;

    internal class GenerateDatabaseCommand 
    {
        public int Codepage { private get; set; }

        public IBackendHelper BackendHelper { private get; set; }

        public IEnumerable<IFileSystemExtension> Extensions { private get; set; }

        /// <summary>
        /// Whether to keep columns added in a transform.
        /// </summary>
        public bool KeepAddedColumns { private get; set; }

        public IMessaging Messaging { private get; set; }

        public WindowsInstallerData Output { private get; set; }

        public string OutputPath { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string IntermediateFolder { private get; set; }

        public List<ITrackedFile> GeneratedTemporaryFiles { get; } = new List<ITrackedFile>();

        /// <summary>
        /// Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.
        /// </summary>
        public bool SuppressAddingValidationRows { private get; set; }

        public bool UseSubDirectory { private get; set; }

        public void Execute()
        {
            // Add the _Validation rows.
            if (!this.SuppressAddingValidationRows)
            {
                var validationTable = this.Output.EnsureTable(this.TableDefinitions["_Validation"]);

                foreach (var table in this.Output.Tables)
                {
                    if (!table.Definition.Unreal)
                    {
                        // Add the validation rows for this table.
                        foreach (ColumnDefinition columnDef in table.Definition.Columns)
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
            }

            // Set the base directory.
            var baseDirectory = this.IntermediateFolder;

            if (this.UseSubDirectory)
            {
                string filename = Path.GetFileNameWithoutExtension(this.OutputPath);
                baseDirectory = Path.Combine(baseDirectory, filename);

                // make sure the directory exists
                Directory.CreateDirectory(baseDirectory);
            }

            var idtDirectory = Path.Combine(baseDirectory, "_idts");
            Directory.CreateDirectory(idtDirectory);

            try
            {
                OpenDatabase type = OpenDatabase.CreateDirect;

                // set special flag for patch files
                if (OutputType.Patch == this.Output.Type)
                {
                    type |= OpenDatabase.OpenPatchFile;
                }

#if DEBUG
                Console.WriteLine("Opening database at: {0}", this.OutputPath);
#endif

                // Localize the codepage if a value was specified directly.
                if (-1 != this.Codepage)
                {
                    this.Output.Codepage = this.Codepage;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(this.OutputPath));

                using (Database db = new Database(this.OutputPath, type))
                {
                    // if we're not using the default codepage, import a new one into our
                    // database before we add any tables (or the tables would be added
                    // with the wrong codepage).
                    if (0 != this.Output.Codepage)
                    {
                        this.SetDatabaseCodepage(db, this.Output.Codepage, idtDirectory);
                    }

                    foreach (Table table in this.Output.Tables)
                    {
                        Table importTable = table;
                        bool hasBinaryColumn = false;

                        // Skip all unreal tables other than _Streams.
                        if (table.Definition.Unreal && "_Streams" != table.Name)
                        {
                            continue;
                        }

                        // Do not put the _Validation table in patches, it is not needed.
                        if (OutputType.Patch == this.Output.Type && "_Validation" == table.Name)
                        {
                            continue;
                        }

                        // The only way to import binary data is to copy it to a local subdirectory first.
                        // To avoid this extra copying and perf hit, import an empty table with the same
                        // definition and later import the binary data from source using records.
                        foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
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
                                var command = new CreateIdtFileCommand(this.Messaging, importTable, this.Output.Codepage, idtDirectory, this.KeepAddedColumns);
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
                            StringBuilder query = new StringBuilder("SELECT ");

                            // Build the query for the view.
                            bool firstColumn = true;
                            foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
                            {
                                if (!firstColumn)
                                {
                                    query.Append(",");
                                }

                                query.AppendFormat(" `{0}`", columnDefinition.Name);
                                firstColumn = false;
                            }
                            query.AppendFormat(" FROM `{0}`", table.Name);

                            using (View tableView = db.OpenExecuteView(query.ToString()))
                            {
                                // Import each row containing a stream
                                foreach (Row row in table.Rows)
                                {
                                    using (Record record = new Record(table.Definition.Columns.Length))
                                    {
                                        StringBuilder streamName = new StringBuilder();
                                        bool needStream = false;

                                        // the _Streams table doesn't prepend the table name (or a period)
                                        if ("_Streams" != table.Name)
                                        {
                                            streamName.Append(table.Name);
                                        }

                                        for (int i = 0; i < table.Definition.Columns.Length; i++)
                                        {
                                            ColumnDefinition columnDefinition = table.Definition.Columns[i];

                                            switch (columnDefinition.Type)
                                            {
                                                case ColumnType.Localized:
                                                case ColumnType.Preserved:
                                                case ColumnType.String:
                                                    if (columnDefinition.PrimaryKey)
                                                    {
                                                        if (0 < streamName.Length)
                                                        {
                                                            streamName.Append(".");
                                                        }
                                                        streamName.Append((string)row[i]);
                                                    }

                                                    record.SetString(i + 1, (string)row[i]);
                                                    break;
                                                case ColumnType.Number:
                                                    record.SetInteger(i + 1, Convert.ToInt32(row[i], CultureInfo.InvariantCulture));
                                                    break;
                                                case ColumnType.Object:
                                                    if (null != row[i])
                                                    {
                                                        needStream = true;
                                                        try
                                                        {
                                                            record.SetStream(i + 1, (string)row[i]);
                                                        }
                                                        catch (Win32Exception e)
                                                        {
                                                            if (0xA1 == e.NativeErrorCode) // ERROR_BAD_PATHNAME
                                                            {
                                                                throw new WixException(ErrorMessages.FileNotFound(row.SourceLineNumbers, (string)row[i]));
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

                                        // stream names are created by concatenating the name of the table with the values
                                        // of the primary key (delimited by periods)
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

                    // Insert substorages (usually transforms inside a patch or instance transforms in a package).
                    if (0 < this.Output.SubStorages.Count)
                    {
                        using (View storagesView = new View(db, "SELECT `Name`, `Data` FROM `_Storages`"))
                        {
                            foreach (SubStorage subStorage in this.Output.SubStorages)
                            {
                                string transformFile = Path.Combine(this.IntermediateFolder, String.Concat(subStorage.Name, ".mst"));

                                // Bind the transform.
                                this.BindTransform(subStorage.Data, transformFile);

                                if (this.Messaging.EncounteredError)
                                {
                                    continue;
                                }

                                // add the storage
                                using (Record record = new Record(2))
                                {
                                    record.SetString(1, subStorage.Name);
                                    record.SetStream(2, transformFile);
                                    storagesView.Modify(ModifyView.Assign, record);
                                }
                            }
                        }
                    }

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

        private void BindTransform(WindowsInstallerData transform, string outputPath)
        {
            var command = new BindTransformCommand();
            command.Messaging = this.Messaging;
            command.Extensions = this.Extensions;
            command.TempFilesLocation = this.IntermediateFolder;
            command.Transform = transform;
            command.OutputPath = outputPath;
            command.TableDefinitions = this.TableDefinitions;
            command.Execute();
        }

        private void SetDatabaseCodepage(Database db, int codepage, string idtDirectory)
        {
            // write out the _ForceCodepage IDT file
            var idtPath = Path.Combine(idtDirectory, "_ForceCodepage.idt");
            using (var idtFile = new StreamWriter(idtPath, false, Encoding.ASCII))
            {
                idtFile.WriteLine(); // dummy column name record
                idtFile.WriteLine(); // dummy column definition record
                idtFile.Write(codepage);
                idtFile.WriteLine("\t_ForceCodepage");
            }

            var trackId = this.BackendHelper.TrackFile(idtPath, TrackedFileType.Temporary);
            this.GeneratedTemporaryFiles.Add(trackId);

            // try to import the table into the MSI
            try
            {
                db.Import(idtPath);
            }
            catch (WixInvalidIdtException)
            {
                // the IDT should be valid, so an invalid code page was given
                throw new WixException(ErrorMessages.IllegalCodepage(codepage));
            }
        }
    }
}
