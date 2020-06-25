// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Creates a transform by diffing two outputs.
    /// </summary>
    public sealed class GenerateTransformCommand
    {
        private const char sectionDelimiter = '/';
        private readonly IMessaging messaging;
        private SummaryInformationStreams transformSummaryInfo;

        /// <summary>
        /// Instantiates a new Differ class.
        /// </summary>
        public GenerateTransformCommand(IMessaging messaging, WindowsInstallerData targetOutput, WindowsInstallerData updatedOutput, bool preserveUnchangedRows, bool showPedanticMessages)
        {
            this.messaging = messaging;
            this.TargetOutput = targetOutput;
            this.UpdatedOutput = updatedOutput;
            this.PreserveUnchangedRows = preserveUnchangedRows;
            this.ShowPedanticMessages = showPedanticMessages;
        }

        private WindowsInstallerData TargetOutput { get; }

        private WindowsInstallerData UpdatedOutput { get; }

        private TransformFlags ValidationFlags { get; }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        private bool ShowPedanticMessages { get; }

        /// <summary>
        /// Gets or sets the option to suppress keeping special rows.
        /// </summary>
        /// <value>The option to suppress keeping special rows.</value>
        private bool SuppressKeepingSpecialRows { get; }

        /// <summary>
        /// Gets or sets the flag to determine if all rows, even unchanged ones will be persisted in the output.
        /// </summary>
        /// <value>The option to keep all rows including unchanged rows.</value>
        private bool PreserveUnchangedRows { get; }

        public WindowsInstallerData Transform { get; private set; }

        /// <summary>
        /// Creates a transform by diffing two outputs.
        /// </summary>
        /// <param name="targetOutput">The target output.</param>
        /// <param name="updatedOutput">The updated output.</param>
        /// <param name="validationFlags"></param>
        /// <returns>The transform.</returns>
        public WindowsInstallerData Execute()
        {
            var targetOutput = this.TargetOutput;
            var updatedOutput = this.UpdatedOutput;
            var validationFlags = this.ValidationFlags;

            var transform = new WindowsInstallerData(null)
            {
                Type = OutputType.Transform,
                Codepage = updatedOutput.Codepage
            };

            this.transformSummaryInfo = new SummaryInformationStreams();

            // compare the codepages
            if (targetOutput.Codepage != updatedOutput.Codepage && 0 == (TransformFlags.ErrorChangeCodePage & validationFlags))
            {
                this.messaging.Write(ErrorMessages.OutputCodepageMismatch(targetOutput.SourceLineNumbers, targetOutput.Codepage, updatedOutput.Codepage));
                if (null != updatedOutput.SourceLineNumbers)
                {
                    this.messaging.Write(ErrorMessages.OutputCodepageMismatch2(updatedOutput.SourceLineNumbers));
                }
            }

            // compare the output types
            if (targetOutput.Type != updatedOutput.Type)
            {
                throw new WixException(ErrorMessages.OutputTypeMismatch(targetOutput.SourceLineNumbers, targetOutput.Type.ToString(), updatedOutput.Type.ToString()));
            }

            // compare the contents of the tables
            foreach (var targetTable in targetOutput.Tables)
            {
                var updatedTable = updatedOutput.Tables[targetTable.Name];
                var operation = TableOperation.None;

                var rows = this.CompareTables(targetOutput, targetTable, updatedTable, out operation);

                if (TableOperation.Drop == operation)
                {
                    var droppedTable = transform.EnsureTable(targetTable.Definition);
                    droppedTable.Operation = TableOperation.Drop;
                }
                else if (TableOperation.None == operation)
                {
                    var modifiedTable = transform.EnsureTable(updatedTable.Definition);
                    foreach (var row in rows)
                    {
                        modifiedTable.Rows.Add(row);
                    }
                }
            }

            // added tables
            foreach (var updatedTable in updatedOutput.Tables)
            {
                if (null == targetOutput.Tables[updatedTable.Name])
                {
                    var addedTable = transform.EnsureTable(updatedTable.Definition);
                    addedTable.Operation = TableOperation.Add;

                    foreach (var updatedRow in updatedTable.Rows)
                    {
                        updatedRow.Operation = RowOperation.Add;
                        updatedRow.SectionId = sectionDelimiter + updatedRow.SectionId;
                        addedTable.Rows.Add(updatedRow);
                    }
                }
            }

            // set summary information properties
            if (!this.SuppressKeepingSpecialRows)
            {
                var summaryInfoTable = transform.Tables["_SummaryInformation"];
                this.UpdateTransformSummaryInformationTable(summaryInfoTable, validationFlags);
            }

            this.Transform = transform;
            return this.Transform;
        }

        /// <summary>
        /// Add a row to the <paramref name="index"/> using the primary key.
        /// </summary>
        /// <param name="index">The indexed rows.</param>
        /// <param name="row">The row to index.</param>
        private void AddIndexedRow(Dictionary<string, Row> index, Row row)
        {
            var primaryKey = row.GetPrimaryKey();

            if (null != primaryKey)
            {
                if (index.TryGetValue(primaryKey, out var collisionRow))
                {
#if TODO_PATCH // This case doesn't seem like it can happen any longer.
                    // Overriding WixActionRows have a primary key defined and take precedence in the index.
                    if (row is WixActionRow actionRow)
                    {
                        // If the current row is not overridable, see if the indexed row is.
                        if (!actionRow.Overridable)
                        {
                            if (collisionRow is WixActionRow indexedRow && indexedRow.Overridable)
                            {
                                // The indexed key is overridable and should be replaced.
                                index[primaryKey] = actionRow;
                            }
                        }

                        // If we got this far, the row does not need to be indexed.
                        return;
                    }
#endif

                    if (this.ShowPedanticMessages)
                    {
                        this.messaging.Write(ErrorMessages.DuplicatePrimaryKey(row.SourceLineNumbers, primaryKey, row.Table.Name));
                    }
                }
                else
                {
                    index.Add(primaryKey, row);
                }
            }
            else // use the string representation of the row as its primary key (it may not be unique)
            {
                // this is provided for compatibility with unreal tables with no primary key
                // all real tables must specify at least one column as the primary key
                primaryKey = row.ToString();
                index[primaryKey] = row;
            }
        }

        private bool CompareRows(Table targetTable, Row targetRow, Row updatedRow, out Row comparedRow)
        {
            comparedRow = null;

            var keepRow = false;

            if (null == targetRow ^ null == updatedRow)
            {
                if (null == targetRow)
                {
                    updatedRow.Operation = RowOperation.Add;
                    comparedRow = updatedRow;
                }
                else if (null == updatedRow)
                {
                    targetRow.Operation = RowOperation.Delete;
                    targetRow.SectionId += sectionDelimiter;

                    comparedRow = targetRow;
                    keepRow = true;
                }
            }
            else // possibly modified
            {
                updatedRow.Operation = RowOperation.None;
                if (!this.SuppressKeepingSpecialRows && "_SummaryInformation" == targetTable.Name)
                {
                    // ignore rows that shouldn't be in a transform
                    if (Enum.IsDefined(typeof(SummaryInformation.Transform), (int)updatedRow[0]))
                    {
                        updatedRow.SectionId = targetRow.SectionId + sectionDelimiter + updatedRow.SectionId;
                        comparedRow = updatedRow;
                        keepRow = true;
                    }
                }
                else
                {
                    if (this.PreserveUnchangedRows)
                    {
                        keepRow = true;
                    }

                    for (var i = 0; i < updatedRow.Fields.Length; i++)
                    {
                        var columnDefinition = updatedRow.Fields[i].Column;

                        if (!columnDefinition.PrimaryKey)
                        {
                            var modified = false;

                            if (i >= targetRow.Fields.Length)
                            {
                                columnDefinition.Added = true;
                                modified = true;
                            }
                            else if (ColumnType.Number == columnDefinition.Type && !columnDefinition.IsLocalizable)
                            {
                                if (null == targetRow[i] ^ null == updatedRow[i])
                                {
                                    modified = true;
                                }
                                else if (null != targetRow[i] && null != updatedRow[i])
                                {
                                    modified = (targetRow.FieldAsInteger(i) != updatedRow.FieldAsInteger(i));
                                }
                            }
                            else if (ColumnType.Preserved == columnDefinition.Type)
                            {
                                updatedRow.Fields[i].PreviousData = targetRow.FieldAsString(i);

                                // keep rows containing preserved fields so the historical data is available to the binder
                                keepRow = !this.SuppressKeepingSpecialRows;
                            }
                            else if (ColumnType.Object == columnDefinition.Type)
                            {
                                var targetObjectField = (ObjectField)targetRow.Fields[i];
                                var updatedObjectField = (ObjectField)updatedRow.Fields[i];

                                updatedObjectField.PreviousEmbeddedFileIndex = targetObjectField.EmbeddedFileIndex;
                                updatedObjectField.PreviousBaseUri = targetObjectField.BaseUri;

                                // always keep a copy of the previous data even if they are identical
                                // This makes diff.wixmst clean and easier to control patch logic
                                updatedObjectField.PreviousData = (string)targetObjectField.Data;

                                // always remember the unresolved data for target build
                                updatedObjectField.UnresolvedPreviousData = targetObjectField.UnresolvedData;

                                // keep rows containing object fields so the files can be compared in the binder
                                keepRow = !this.SuppressKeepingSpecialRows;
                            }
                            else
                            {
                                modified = (targetRow.FieldAsString(i) != updatedRow.FieldAsString(i));
                            }

                            if (modified)
                            {
                                if (null != updatedRow.Fields[i].PreviousData)
                                {
                                    updatedRow.Fields[i].PreviousData = targetRow.FieldAsString(i);
                                }

                                updatedRow.Fields[i].Modified = true;
                                updatedRow.Operation = RowOperation.Modify;
                                keepRow = true;
                            }
                        }
                    }

                    if (keepRow)
                    {
                        comparedRow = updatedRow;
                        comparedRow.SectionId = targetRow.SectionId + sectionDelimiter + updatedRow.SectionId;
                    }
                }
            }

            return keepRow;
        }

        private List<Row> CompareTables(WindowsInstallerData targetOutput, Table targetTable, Table updatedTable, out TableOperation operation)
        {
            var rows = new List<Row>();
            operation = TableOperation.None;

            // dropped tables
            if (null == updatedTable ^ null == targetTable)
            {
                if (null == targetTable)
                {
                    operation = TableOperation.Add;
                    rows.AddRange(updatedTable.Rows);
                }
                else if (null == updatedTable)
                {
                    operation = TableOperation.Drop;
                }
            }
            else // possibly modified tables
            {
                var updatedPrimaryKeys = new Dictionary<string, Row>();
                var targetPrimaryKeys = new Dictionary<string, Row>();

                // compare the table definitions
                if (0 != targetTable.Definition.CompareTo(updatedTable.Definition))
                {
                    // continue to the next table; may be more mismatches
                    this.messaging.Write(ErrorMessages.DatabaseSchemaMismatch(targetOutput.SourceLineNumbers, targetTable.Name));
                }
                else
                {
                    this.IndexPrimaryKeys(targetTable, targetPrimaryKeys, updatedTable, updatedPrimaryKeys);

                    // diff the target and updated rows
                    foreach (var targetPrimaryKeyEntry in targetPrimaryKeys)
                    {
                        var targetPrimaryKey = targetPrimaryKeyEntry.Key;
                        var targetRow = targetPrimaryKeyEntry.Value;
                        updatedPrimaryKeys.TryGetValue(targetPrimaryKey, out var updatedRow);

                        var keepRow = this.CompareRows(targetTable, targetRow, updatedRow, out var compared);

                        if (keepRow)
                        {
                            rows.Add(compared);
                        }
                    }

                    // find the inserted rows
                    foreach (var updatedPrimaryKeyEntry in updatedPrimaryKeys)
                    {
                        var updatedPrimaryKey = updatedPrimaryKeyEntry.Key;

                        if (!targetPrimaryKeys.ContainsKey(updatedPrimaryKey))
                        {
                            var updatedRow = updatedPrimaryKeyEntry.Value;

                            updatedRow.Operation = RowOperation.Add;
                            updatedRow.SectionId = sectionDelimiter + updatedRow.SectionId;
                            rows.Add(updatedRow);
                        }
                    }
                }
            }

            return rows;
        }

        private void IndexPrimaryKeys(Table targetTable, Dictionary<string, Row> targetPrimaryKeys, Table updatedTable, Dictionary<string, Row> updatedPrimaryKeys)
        {
            // index the target rows
            foreach (var row in targetTable.Rows)
            {
                this.AddIndexedRow(targetPrimaryKeys, row);

                if ("Property" == targetTable.Name)
                {
                    var id = row.FieldAsString(0);

                    if ("ProductCode" == id)
                    {
                        this.transformSummaryInfo.TargetProductCode = row.FieldAsString(1);

                        if ("*" == this.transformSummaryInfo.TargetProductCode)
                        {
                            this.messaging.Write(ErrorMessages.ProductCodeInvalidForTransform(row.SourceLineNumbers));
                        }
                    }
                    else if ("ProductVersion" == id)
                    {
                        this.transformSummaryInfo.TargetProductVersion = row.FieldAsString(1);
                    }
                    else if ("UpgradeCode" == id)
                    {
                        this.transformSummaryInfo.TargetUpgradeCode = row.FieldAsString(1);
                    }
                }
                else if ("_SummaryInformation" == targetTable.Name)
                {
                    var id = row.FieldAsInteger(0);

                    if (1 == id) // PID_CODEPAGE
                    {
                        this.transformSummaryInfo.TargetSummaryInfoCodepage = row.FieldAsString(1);
                    }
                    else if (7 == id) // PID_TEMPLATE
                    {
                        this.transformSummaryInfo.TargetPlatformAndLanguage = row.FieldAsString(1);
                    }
                    else if (14 == id) // PID_PAGECOUNT
                    {
                        this.transformSummaryInfo.TargetMinimumVersion = row.FieldAsString(1);
                    }
                }
            }

            // index the updated rows
            foreach (var row in updatedTable.Rows)
            {
                this.AddIndexedRow(updatedPrimaryKeys, row);

                if ("Property" == updatedTable.Name)
                {
                    var id = row.FieldAsString(0);

                    if ("ProductCode" == id)
                    {
                        this.transformSummaryInfo.UpdatedProductCode = row.FieldAsString(1);

                        if ("*" == this.transformSummaryInfo.UpdatedProductCode)
                        {
                            this.messaging.Write(ErrorMessages.ProductCodeInvalidForTransform(row.SourceLineNumbers));
                        }
                    }
                    else if ("ProductVersion" == id)
                    {
                        this.transformSummaryInfo.UpdatedProductVersion = row.FieldAsString(1);
                    }
                }
                else if ("_SummaryInformation" == updatedTable.Name)
                {
                    var id = row.FieldAsInteger(0);

                    if (1 == id) // PID_CODEPAGE
                    {
                        this.transformSummaryInfo.UpdatedSummaryInfoCodepage = row.FieldAsString(1);
                    }
                    else if (7 == id) // PID_TEMPLATE
                    {
                        this.transformSummaryInfo.UpdatedPlatformAndLanguage = row.FieldAsString(1);
                    }
                    else if (14 == id) // PID_PAGECOUNT
                    {
                        this.transformSummaryInfo.UpdatedMinimumVersion = row.FieldAsString(1);
                    }
                }
            }
        }

        private void UpdateTransformSummaryInformationTable(Table summaryInfoTable, TransformFlags validationFlags)
        {
            // calculate the minimum version of MSI required to process the transform
            var minimumVersion = 100;

            if (Int32.TryParse(this.transformSummaryInfo.TargetMinimumVersion, out var targetMin) && Int32.TryParse(this.transformSummaryInfo.UpdatedMinimumVersion, out var updatedMin))
            {
                minimumVersion = Math.Max(targetMin, updatedMin);
            }

            var summaryRows = new Dictionary<int, Row>(summaryInfoTable.Rows.Count);

            foreach (var row in summaryInfoTable.Rows)
            {
                var id = row.FieldAsInteger(0);

                summaryRows[id] = row;

                if ((int)SummaryInformation.Transform.CodePage == id)
                {
                    row.Fields[1].Data = this.transformSummaryInfo.UpdatedSummaryInfoCodepage;
                    row.Fields[1].PreviousData = this.transformSummaryInfo.TargetSummaryInfoCodepage;
                }
                else if ((int)SummaryInformation.Transform.TargetPlatformAndLanguage == id)
                {
                    row[1] = this.transformSummaryInfo.TargetPlatformAndLanguage;
                }
                else if ((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == id)
                {
                    row[1] = this.transformSummaryInfo.UpdatedPlatformAndLanguage;
                }
                else if ((int)SummaryInformation.Transform.ProductCodes == id)
                {
                    row[1] = String.Concat(this.transformSummaryInfo.TargetProductCode, this.transformSummaryInfo.TargetProductVersion, ';', this.transformSummaryInfo.UpdatedProductCode, this.transformSummaryInfo.UpdatedProductVersion, ';', this.transformSummaryInfo.TargetUpgradeCode);
                }
                else if ((int)SummaryInformation.Transform.InstallerRequirement == id)
                {
                    row[1] = minimumVersion.ToString(CultureInfo.InvariantCulture);
                }
                else if ((int)SummaryInformation.Transform.Security == id)
                {
                    row[1] = "4";
                }
            }

            if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.TargetPlatformAndLanguage))
            {
                var summaryRow = summaryInfoTable.CreateRow(null);
                summaryRow[0] = (int)SummaryInformation.Transform.TargetPlatformAndLanguage;
                summaryRow[1] = this.transformSummaryInfo.TargetPlatformAndLanguage;
            }

            if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage))
            {
                var summaryRow = summaryInfoTable.CreateRow(null);
                summaryRow[0] = (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage;
                summaryRow[1] = this.transformSummaryInfo.UpdatedPlatformAndLanguage;
            }

            if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.ValidationFlags))
            {
                var summaryRow = summaryInfoTable.CreateRow(null);
                summaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                summaryRow[1] = ((int)validationFlags).ToString(CultureInfo.InvariantCulture);
            }

            if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.InstallerRequirement))
            {
                var summaryRow = summaryInfoTable.CreateRow(null);
                summaryRow[0] = (int)SummaryInformation.Transform.InstallerRequirement;
                summaryRow[1] = minimumVersion.ToString(CultureInfo.InvariantCulture);
            }

            if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.Security))
            {
                var summaryRow = summaryInfoTable.CreateRow(null);
                summaryRow[0] = (int)SummaryInformation.Transform.Security;
                summaryRow[1] = "4";
            }
        }

        private class SummaryInformationStreams
        {
            public string TargetSummaryInfoCodepage { get; set; }

            public string TargetPlatformAndLanguage { get; set; }

            public string TargetProductCode { get; set; }

            public string TargetProductVersion { get; set; }

            public string TargetUpgradeCode { get; set; }

            public string TargetMinimumVersion { get; set; }

            public string UpdatedSummaryInfoCodepage { get; set; }

            public string UpdatedPlatformAndLanguage { get; set; }

            public string UpdatedProductCode { get; set; }

            public string UpdatedProductVersion { get; set; }

            public string UpdatedMinimumVersion { get; set; }
        }
    }
}
