// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Globalization;
    using System.IO;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class BindTransformCommand
    {
        public BindTransformCommand(IMessaging messaging, IBackendHelper backendHelper, FileSystemManager fileSystemManager, string intermediateFolder, WindowsInstallerData transform, string outputPath, TableDefinitionCollection tableDefinitions)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystemManager = fileSystemManager;
            this.IntermediateFolder = intermediateFolder;
            this.Transform = transform;
            this.OutputPath = outputPath;
            this.TableDefinitions = tableDefinitions;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private FileSystemManager FileSystemManager { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private string IntermediateFolder { get; }

        private WindowsInstallerData Transform { get; }

        private string OutputPath { get; }

        public void Execute()
        {
            var transformFlags = 0;

            var targetOutput = new WindowsInstallerData(null);
            var updatedOutput = new WindowsInstallerData(null);

            // TODO: handle added columns

            // to generate a localized transform, both the target and updated
            // databases need to have the same code page. the only reason to
            // set different code pages is to support localized primary key
            // columns, but that would only support deleting rows. if this
            // becomes necessary, define a PreviousCodepage property on the
            // Output class and persist this throughout transform generation.
            targetOutput.Codepage = this.Transform.Codepage;
            updatedOutput.Codepage = this.Transform.Codepage;

            // remove certain Property rows which will be populated from summary information values
            string targetUpgradeCode = null;
            string updatedUpgradeCode = null;

            if (this.Transform.TryGetTable("Property", out var propertyTable))
            {
                for (int i = propertyTable.Rows.Count - 1; i >= 0; i--)
                {
                    Row row = propertyTable.Rows[i];

                    if ("ProductCode" == (string)row[0] || "ProductLanguage" == (string)row[0] || "ProductVersion" == (string)row[0] || "UpgradeCode" == (string)row[0])
                    {
                        propertyTable.Rows.RemoveAt(i);

                        if ("UpgradeCode" == (string)row[0])
                        {
                            updatedUpgradeCode = (string)row[1];
                        }
                    }
                }
            }

            var targetSummaryInfo = targetOutput.EnsureTable(this.TableDefinitions["_SummaryInformation"]);
            var updatedSummaryInfo = updatedOutput.EnsureTable(this.TableDefinitions["_SummaryInformation"]);
            var targetPropertyTable = targetOutput.EnsureTable(this.TableDefinitions["Property"]);
            var updatedPropertyTable = updatedOutput.EnsureTable(this.TableDefinitions["Property"]);

            // process special summary information values
            foreach (var row in this.Transform.Tables["_SummaryInformation"].Rows)
            {
                var summaryId = row.FieldAsInteger(0);
                var summaryData = row.FieldAsString(1);

                if ((int)SummaryInformation.Transform.CodePage == summaryId)
                {
                    // convert from a web name if provided
                    var codePage = summaryData;
                    if (null == codePage)
                    {
                        codePage = "0";
                    }
                    else
                    {
                        codePage = this.BackendHelper.GetValidCodePage(codePage).ToString(CultureInfo.InvariantCulture);
                    }

                    var previousCodePage = row.Fields[1].PreviousData;
                    if (null == previousCodePage)
                    {
                        previousCodePage = "0";
                    }
                    else
                    {
                        previousCodePage = this.BackendHelper.GetValidCodePage(previousCodePage).ToString(CultureInfo.InvariantCulture);
                    }

                    var targetCodePageRow = targetSummaryInfo.CreateRow(null);
                    targetCodePageRow[0] = 1; // PID_CODEPAGE
                    targetCodePageRow[1] = previousCodePage;

                    var updatedCodePageRow = updatedSummaryInfo.CreateRow(null);
                    updatedCodePageRow[0] = 1; // PID_CODEPAGE
                    updatedCodePageRow[1] = codePage;
                }
                else if ((int)SummaryInformation.Transform.TargetPlatformAndLanguage == summaryId ||
                         (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == summaryId)
                {
                    // the target language
                    var propertyData = summaryData.Split(';');
                    var lang = 2 == propertyData.Length ? propertyData[1] : "0";

                    var tempSummaryInfo = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == summaryId ? targetSummaryInfo : updatedSummaryInfo;
                    var tempPropertyTable = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == summaryId ? targetPropertyTable : updatedPropertyTable;

                    var productLanguageRow = tempPropertyTable.CreateRow(null);
                    productLanguageRow[0] = "ProductLanguage";
                    productLanguageRow[1] = lang;

                    // set the platform;language on the MSI to be generated
                    var templateRow = tempSummaryInfo.CreateRow(null);
                    templateRow[0] = 7; // PID_TEMPLATE
                    templateRow[1] = summaryData;
                }
                else if ((int)SummaryInformation.Transform.ProductCodes == summaryId)
                {
                    var propertyData = summaryData.Split(';');

                    var targetProductCodeRow = targetPropertyTable.CreateRow(null);
                    targetProductCodeRow[0] = "ProductCode";
                    targetProductCodeRow[1] = propertyData[0].Substring(0, 38);

                    var targetProductVersionRow = targetPropertyTable.CreateRow(null);
                    targetProductVersionRow[0] = "ProductVersion";
                    targetProductVersionRow[1] = propertyData[0].Substring(38);

                    var updatedProductCodeRow = updatedPropertyTable.CreateRow(null);
                    updatedProductCodeRow[0] = "ProductCode";
                    updatedProductCodeRow[1] = propertyData[1].Substring(0, 38);

                    var updatedProductVersionRow = updatedPropertyTable.CreateRow(null);
                    updatedProductVersionRow[0] = "ProductVersion";
                    updatedProductVersionRow[1] = propertyData[1].Substring(38);

                    // UpgradeCode is optional and may not exists in the target
                    // or upgraded databases, so do not include a null-valued
                    // UpgradeCode property.

                    targetUpgradeCode = propertyData[2];
                    if (!String.IsNullOrEmpty(targetUpgradeCode))
                    {
                        var targetUpgradeCodeRow = targetPropertyTable.CreateRow(null);
                        targetUpgradeCodeRow[0] = "UpgradeCode";
                        targetUpgradeCodeRow[1] = targetUpgradeCode;

                        // If the target UpgradeCode is specified, an updated
                        // UpgradeCode is required.
                        if (String.IsNullOrEmpty(updatedUpgradeCode))
                        {
                            updatedUpgradeCode = targetUpgradeCode;
                        }
                    }

                    if (!String.IsNullOrEmpty(updatedUpgradeCode))
                    {
                        var updatedUpgradeCodeRow = updatedPropertyTable.CreateRow(null);
                        updatedUpgradeCodeRow[0] = "UpgradeCode";
                        updatedUpgradeCodeRow[1] = updatedUpgradeCode;
                    }
                }
                else if ((int)SummaryInformation.Transform.ValidationFlags == summaryId)
                {
                    transformFlags = Convert.ToInt32(summaryData, CultureInfo.InvariantCulture);
                }
                else if ((int)SummaryInformation.Transform.Reserved11 == summaryId)
                {
                    // PID_LASTPRINTED should be null for transforms
                    row.Operation = RowOperation.None;
                }
                else
                {
                    // add everything else as is
                    var targetRow = targetSummaryInfo.CreateRow(null);
                    targetRow[0] = row[0];
                    targetRow[1] = row[1];

                    var updatedRow = updatedSummaryInfo.CreateRow(null);
                    updatedRow[0] = row[0];
                    updatedRow[1] = row[1];
                }
            }

            // Validate that both databases have an UpgradeCode if the
            // authoring transform will validate the UpgradeCode; otherwise,
            // MsiCreateTransformSummaryinfo() will fail with 1620.
            if (((int)TransformFlags.ValidateUpgradeCode & transformFlags) != 0 &&
                (String.IsNullOrEmpty(targetUpgradeCode) || String.IsNullOrEmpty(updatedUpgradeCode)))
            {
                this.Messaging.Write(ErrorMessages.BothUpgradeCodesRequired());
            }

            string emptyFile = null;

            foreach (var table in this.Transform.Tables)
            {
                // Ignore unreal tables when building transforms except the _Stream table.
                // These tables are ignored when generating the database so there is no reason
                // to process them here.
                if (table.Definition.Unreal && "_Streams" != table.Name)
                {
                    continue;
                }

                // process table operations
                switch (table.Operation)
                {
                    case TableOperation.Add:
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                    case TableOperation.Drop:
                        targetOutput.EnsureTable(table.Definition);
                        continue;
                    default:
                        targetOutput.EnsureTable(table.Definition);
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                }

                // process row operations
                foreach (var row in table.Rows)
                {
                    switch (row.Operation)
                    {
                        case RowOperation.Add:
                            var updatedTable = updatedOutput.EnsureTable(table.Definition);
                            updatedTable.Rows.Add(row);
                            continue;

                        case RowOperation.Delete:
                            var targetTable = targetOutput.EnsureTable(table.Definition);
                            targetTable.Rows.Add(row);

                            // fill-in non-primary key values
                            foreach (var field in row.Fields)
                            {
                                if (!field.Column.PrimaryKey)
                                {
                                    if (ColumnType.Number == field.Column.Type && !field.Column.IsLocalizable)
                                    {
                                        field.Data = field.Column.MinValue;
                                    }
                                    else if (ColumnType.Object == field.Column.Type)
                                    {
                                        if (null == emptyFile)
                                        {
                                            emptyFile = Path.Combine(this.IntermediateFolder, "empty");
                                        }

                                        field.Data = emptyFile;
                                    }
                                    else
                                    {
                                        field.Data = "0";
                                    }
                                }
                            }
                            continue;
                    }

                    // Assure that the file table's sequence is populated
                    if ("File" == table.Name)
                    {
                        foreach (var fileRow in table.Rows)
                        {
                            if (null == fileRow[7])
                            {
                                if (RowOperation.Add == fileRow.Operation)
                                {
                                    this.Messaging.Write(ErrorMessages.InvalidAddedFileRowWithoutSequence(fileRow.SourceLineNumbers, (string)fileRow[0]));
                                    break;
                                }

                                // Set to 1 to prevent invalid IDT file from being generated
                                fileRow[7] = 1;
                            }
                        }
                    }

                    // process modified and unmodified rows
                    var modifiedRow = false;
                    var targetRow = table.Definition.CreateRow(null);
                    var updatedRow = row;
                    for (var i = 0; i < row.Fields.Length; i++)
                    {
                        var updatedField = row.Fields[i];

                        if (updatedField.Modified)
                        {
                            // set a different value in the target row to ensure this value will be modified during transform generation
                            if (ColumnType.Number == updatedField.Column.Type && !updatedField.Column.IsLocalizable)
                            {
                                var data = updatedField.AsNullableInteger();
                                targetRow[i] = (data == 1) ? 2 : 1;
                            }
                            else if (ColumnType.Object == updatedField.Column.Type)
                            {
                                if (null == emptyFile)
                                {
                                    emptyFile = Path.Combine(this.IntermediateFolder, "empty");
                                }

                                targetRow[i] = emptyFile;
                            }
                            else
                            {
                                var data = updatedField.AsString();
                                targetRow[i] = (data == "0") ? "1" : "0";
                            }

                            modifiedRow = true;
                        }
                        else if (ColumnType.Object == updatedField.Column.Type)
                        {
                            var objectField = (ObjectField)updatedField;

                            // create an empty file for comparing against
                            if (null == objectField.PreviousData)
                            {
                                if (null == emptyFile)
                                {
                                    emptyFile = Path.Combine(this.IntermediateFolder, "empty");
                                }

                                targetRow[i] = emptyFile;
                                modifiedRow = true;
                            }
                            else if (!this.FileSystemManager.CompareFiles(objectField.PreviousData, (string)objectField.Data))
                            {
                                targetRow[i] = objectField.PreviousData;
                                modifiedRow = true;
                            }
                        }
                        else // unmodified
                        {
                            if (null != updatedField.Data)
                            {
                                targetRow[i] = updatedField.Data;
                            }
                        }
                    }

                    // modified rows and certain special rows go in the target and updated msi databases
                    if (modifiedRow ||
                        ("Property" == table.Name &&
                            ("ProductCode" == (string)row[0] ||
                            "ProductLanguage" == (string)row[0] ||
                            "ProductVersion" == (string)row[0] ||
                            "UpgradeCode" == (string)row[0])))
                    {
                        var targetTable = targetOutput.EnsureTable(table.Definition);
                        targetTable.Rows.Add(targetRow);

                        var updatedTable = updatedOutput.EnsureTable(table.Definition);
                        updatedTable.Rows.Add(updatedRow);
                    }
                }
            }

            //foreach (BinderExtension extension in this.Extensions)
            //{
            //    extension.PostBind(this.Context);
            //}

            // Any errors encountered up to this point can cause errors during generation.
            if (this.Messaging.EncounteredError)
            {
                return;
            }

            var transformFileName = Path.GetFileNameWithoutExtension(this.OutputPath);
            var targetDatabaseFile = Path.Combine(this.IntermediateFolder, String.Concat(transformFileName, "_target.msi"));
            var updatedDatabaseFile = Path.Combine(this.IntermediateFolder, String.Concat(transformFileName, "_updated.msi"));

            try
            {
                if (!String.IsNullOrEmpty(emptyFile))
                {
                    using (var fileStream = File.Create(emptyFile))
                    {
                    }
                }

                this.GenerateDatabase(targetOutput, targetDatabaseFile, keepAddedColumns: false);
                this.GenerateDatabase(updatedOutput, updatedDatabaseFile, keepAddedColumns: true);

                // make sure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(this.OutputPath));

                // create the transform file
                using (var targetDatabase = new Database(targetDatabaseFile, OpenDatabase.ReadOnly))
                using (var updatedDatabase = new Database(updatedDatabaseFile, OpenDatabase.ReadOnly))
                {
                    if (updatedDatabase.GenerateTransform(targetDatabase, this.OutputPath))
                    {
                        updatedDatabase.CreateTransformSummaryInfo(targetDatabase, this.OutputPath, (TransformErrorConditions)(transformFlags & 0xFFFF), (TransformValidations)((transformFlags >> 16) & 0xFFFF));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.NoDifferencesInTransform(this.Transform.SourceLineNumbers));
                    }
                }
            }
            finally
            {
                if (!String.IsNullOrEmpty(emptyFile))
                {
                    File.Delete(emptyFile);
                }
            }
        }

        private void GenerateDatabase(WindowsInstallerData output, string outputPath, bool keepAddedColumns)
        {
            var command = new GenerateDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystemManager, output, outputPath, this.TableDefinitions, this.IntermediateFolder, codepage: -1, keepAddedColumns, suppressAddingValidationRows: true, useSubdirectory: true);
            command.Execute();
        }
    }
}
