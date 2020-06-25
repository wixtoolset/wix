// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Include transforms in a patch.
    /// </summary>
    internal class AttachPatchTransformsCommand
    {
        private static readonly string[] PatchUninstallBreakingTables = new[]
        {
            "AppId",
            "BindImage",
            "Class",
            "Complus",
            "CreateFolder",
            "DuplicateFile",
            "Environment",
            "Extension",
            "Font",
            "IniFile",
            "IsolatedComponent",
            "LockPermissions",
            "MIME",
            "MoveFile",
            "MsiLockPermissionsEx",
            "MsiServiceConfig",
            "MsiServiceConfigFailureActions",
            "ODBCAttribute",
            "ODBCDataSource",
            "ODBCDriver",
            "ODBCSourceAttribute",
            "ODBCTranslator",
            "ProgId",
            "PublishComponent",
            "RemoveIniFile",
            "SelfReg",
            "ServiceControl",
            "ServiceInstall",
            "TypeLib",
            "Verb",
        };

        private readonly TableDefinitionCollection tableDefinitions;

        public AttachPatchTransformsCommand(IMessaging messaging, Intermediate intermediate, IEnumerable<PatchTransform> transforms)
        {
            this.tableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
            this.Messaging = messaging;
            this.Intermediate = intermediate;
            this.Transforms = transforms;
        }

        private IMessaging Messaging { get; }

        private Intermediate Intermediate { get; }

        private IEnumerable<PatchTransform> Transforms { get; }

        public IEnumerable<SubStorage> SubStorages { get; private set; }

        public IEnumerable<SubStorage> Execute()
        {
            var subStorages = new List<SubStorage>();

            if (this.Transforms == null || !this.Transforms.Any())
            {
                this.Messaging.Write(ErrorMessages.PatchWithoutTransforms());
                return subStorages;
            }

            var summaryInfo = this.ExtractPatchSummaryInfo();

            var section = this.Intermediate.Sections.First();

            var symbols = this.Intermediate.Sections.SelectMany(s => s.Symbols).ToList();

            // Get the patch id from the WixPatchId symbol.
            var patchIdSymbol = symbols.OfType<WixPatchIdSymbol>().FirstOrDefault();

            if (String.IsNullOrEmpty(patchIdSymbol.Id?.Id))
            {
                this.Messaging.Write(ErrorMessages.ExpectedPatchIdInWixMsp());
                return subStorages;
            }

            if (String.IsNullOrEmpty(patchIdSymbol.ClientPatchId))
            {
                this.Messaging.Write(ErrorMessages.ExpectedClientPatchIdInWixMsp());
                return subStorages;
            }

            // enumerate patch.Media to map diskId to Media row
            var patchMediaByDiskId = symbols.OfType<MediaSymbol>().ToDictionary(t => t.DiskId);

            if (patchMediaByDiskId.Count == 0)
            {
                this.Messaging.Write(ErrorMessages.ExpectedMediaRowsInWixMsp());
                return subStorages;
            }

            // populate MSP summary information
            var patchMetadata = this.PopulateSummaryInformation(summaryInfo, symbols, patchIdSymbol, section.Codepage);

            // enumerate transforms
            var productCodes = new SortedSet<string>();
            var transformNames = new List<string>();
            var validTransform = new List<Tuple<string, WindowsInstallerData>>();

            var baselineSymbolsById = symbols.OfType<WixPatchBaselineSymbol>().ToDictionary(t => t.Id.Id);

            foreach (var mainTransform in this.Transforms)
            {
                var baselineSymbol = baselineSymbolsById[mainTransform.Baseline];

                var patchRefSymbols = symbols.OfType<WixPatchRefSymbol>().ToList();
                if (patchRefSymbols.Count > 0)
                {
                    if (!this.ReduceTransform(mainTransform.Transform, patchRefSymbols))
                    {
                        // transform has none of the content authored into this patch
                        continue;
                    }
                }

                // Validate the transform doesn't break any patch specific rules.
                this.Validate(mainTransform);

                // ensure consistent File.Sequence within each Media
                var mediaSymbol = patchMediaByDiskId[baselineSymbol.DiskId];

                // Ensure that files are sequenced after the last file in any transform.
                var transformMediaTable = mainTransform.Transform.Tables["Media"];
                if (null != transformMediaTable && 0 < transformMediaTable.Rows.Count)
                {
                    foreach (MediaRow transformMediaRow in transformMediaTable.Rows)
                    {
                        if (!mediaSymbol.LastSequence.HasValue || mediaSymbol.LastSequence < transformMediaRow.LastSequence)
                        {
                            // The Binder will pre-increment the sequence.
                            mediaSymbol.LastSequence = transformMediaRow.LastSequence;
                        }
                    }
                }

                // Use the Media/@DiskId if greater than the last sequence for backward compatibility.
                if (!mediaSymbol.LastSequence.HasValue || mediaSymbol.LastSequence < mediaSymbol.DiskId)
                {
                    mediaSymbol.LastSequence = mediaSymbol.DiskId;
                }

                // Ignore media table in the transform.
                mainTransform.Transform.Tables.Remove("Media");
                mainTransform.Transform.Tables.Remove("MsiDigitalSignature");

                var pairedTransform = this.BuildPairedTransform(summaryInfo, patchMetadata, patchIdSymbol, mainTransform.Transform, mediaSymbol, baselineSymbol, out var productCode);

                productCode = productCode.ToUpperInvariant();
                productCodes.Add(productCode);
                validTransform.Add(Tuple.Create(productCode, mainTransform.Transform));

                // attach these transforms to the patch object
                // TODO: is this an acceptable way to auto-generate transform stream names?
                var transformName = mainTransform.Baseline + "." + validTransform.Count.ToString(CultureInfo.InvariantCulture);
                subStorages.Add(new SubStorage(transformName, mainTransform.Transform));
                subStorages.Add(new SubStorage("#" + transformName, pairedTransform));

                transformNames.Add(":" + transformName);
                transformNames.Add(":#" + transformName);
            }

            if (validTransform.Count == 0)
            {
                this.Messaging.Write(ErrorMessages.PatchWithoutValidTransforms());
                return subStorages;
            }

            // Validate that a patch authored as removable is actually removable
            if (patchMetadata.TryGetValue("AllowRemoval", out var allowRemoval) && allowRemoval.Value == "1")
            {
                var uninstallable = true;

                foreach (var entry in validTransform)
                {
                    uninstallable &= this.CheckUninstallableTransform(entry.Item1, entry.Item2);
                }

                if (!uninstallable)
                {
                    this.Messaging.Write(ErrorMessages.PatchNotRemovable());
                    return subStorages;
                }
            }

            // Finish filling tables with transform-dependent data.
            productCodes = FinalizePatchProductCodes(symbols, productCodes);

            // Semicolon delimited list of the product codes that can accept the patch.
            summaryInfo.Add(SummaryInformationType.PatchProductCodes, new SummaryInformationSymbol(patchIdSymbol.SourceLineNumbers)
            {
                PropertyId = SummaryInformationType.PatchProductCodes,
                Value = String.Join(";", productCodes)
            });

            // Semicolon delimited list of transform substorage names in the order they are applied.
            summaryInfo.Add(SummaryInformationType.TransformNames, new SummaryInformationSymbol(patchIdSymbol.SourceLineNumbers)
            {
                PropertyId = SummaryInformationType.TransformNames,
                Value = String.Join(";", transformNames)
            });

            // Put the summary information that was extracted back in now that it is updated.
            foreach (var readSummaryInfo in summaryInfo.Values.OrderBy(s => s.PropertyId))
            {
                section.AddSymbol(readSummaryInfo);
            }

            this.SubStorages = subStorages;

            return subStorages;
        }

        private Dictionary<SummaryInformationType, SummaryInformationSymbol> ExtractPatchSummaryInfo()
        {
            var result = new Dictionary<SummaryInformationType, SummaryInformationSymbol>();

            foreach (var section in this.Intermediate.Sections)
            {
                for (var i = section.Symbols.Count - 1; i >= 0; i--)
                {
                    if (section.Symbols[i] is SummaryInformationSymbol patchSummaryInfo)
                    {
                        // Remove all summary information from the symbols and remember those that
                        // are not calculated or reserved.
                        section.Symbols.RemoveAt(i);

                        if (patchSummaryInfo.PropertyId != SummaryInformationType.PatchProductCodes &&
                            patchSummaryInfo.PropertyId != SummaryInformationType.PatchCode &&
                            patchSummaryInfo.PropertyId != SummaryInformationType.PatchInstallerRequirement &&
                            patchSummaryInfo.PropertyId != SummaryInformationType.Reserved11 &&
                            patchSummaryInfo.PropertyId != SummaryInformationType.Reserved14 &&
                            patchSummaryInfo.PropertyId != SummaryInformationType.Reserved16)
                        {
                            result.Add(patchSummaryInfo.PropertyId, patchSummaryInfo);
                        }
                    }
                }
            }

            return result;
        }

        private Dictionary<string, MsiPatchMetadataSymbol> PopulateSummaryInformation(Dictionary<SummaryInformationType, SummaryInformationSymbol> summaryInfo, List<IntermediateSymbol> symbols, WixPatchIdSymbol patchIdSymbol, int codepage)
        {
            // PID_CODEPAGE
            if (!summaryInfo.ContainsKey(SummaryInformationType.Codepage))
            {
                // Set the code page by default to the same code page for the
                // string pool in the database.
                AddSummaryInformation(SummaryInformationType.Codepage, codepage.ToString(CultureInfo.InvariantCulture), patchIdSymbol.SourceLineNumbers);
            }

            // GUID patch code for the patch.
            AddSummaryInformation(SummaryInformationType.PatchCode, patchIdSymbol.Id.Id, patchIdSymbol.SourceLineNumbers);

            // Indicates the minimum Windows Installer version that is required to install the patch.
            AddSummaryInformation(SummaryInformationType.PatchInstallerRequirement, ((int)SummaryInformation.InstallerRequirement.Version31).ToString(CultureInfo.InvariantCulture), patchIdSymbol.SourceLineNumbers);

            if (!summaryInfo.ContainsKey(SummaryInformationType.Security))
            {
                AddSummaryInformation(SummaryInformationType.Security, "4", patchIdSymbol.SourceLineNumbers); // Read-only enforced;
            }

            // Use authored comments or default to display name.
            MsiPatchMetadataSymbol commentsSymbol = null;

            var metadataSymbols = symbols.OfType<MsiPatchMetadataSymbol>().Where(t => String.IsNullOrEmpty(t.Company)).ToDictionary(t => t.Property);

            if (!summaryInfo.ContainsKey(SummaryInformationType.Title) &&
                metadataSymbols.TryGetValue("DisplayName", out var displayName))
            {
                AddSummaryInformation(SummaryInformationType.Title, displayName.Value, displayName.SourceLineNumbers);

                // Default comments to use display name as-is.
                commentsSymbol = displayName;
            }

            // TODO: This code below seems unnecessary given the codepage is set at the top of this method.
            //if (!summaryInfo.ContainsKey(SummaryInformationType.Codepage) &&
            //    metadataValues.TryGetValue("CodePage", out var codepage))
            //{
            //    AddSummaryInformation(SummaryInformationType.Codepage, codepage);
            //}

            if (!summaryInfo.ContainsKey(SummaryInformationType.PatchPackageName) &&
                metadataSymbols.TryGetValue("Description", out var description))
            {
                AddSummaryInformation(SummaryInformationType.PatchPackageName, description.Value, description.SourceLineNumbers);
            }

            if (!summaryInfo.ContainsKey(SummaryInformationType.Author) &&
                metadataSymbols.TryGetValue("ManufacturerName", out var manufacturer))
            {
                AddSummaryInformation(SummaryInformationType.Author, manufacturer.Value, manufacturer.SourceLineNumbers);
            }

            // Special metadata marshalled through the build.
            //var wixMetadataValues = symbols.OfType<WixPatchMetadataSymbol>().ToDictionary(t => t.Id.Id, t => t.Value);

            //if (wixMetadataValues.TryGetValue("Comments", out var wixComments))
            if (metadataSymbols.TryGetValue("Comments", out var wixComments))
            {
                commentsSymbol = wixComments;
            }

            // Write the package comments to summary info.
            if (!summaryInfo.ContainsKey(SummaryInformationType.Comments) &&
                commentsSymbol != null)
            {
                AddSummaryInformation(SummaryInformationType.Comments, commentsSymbol.Value, commentsSymbol.SourceLineNumbers);
            }

            return metadataSymbols;

            void AddSummaryInformation(SummaryInformationType type, string value, SourceLineNumber sourceLineNumber)
            {
                summaryInfo.Add(type, new SummaryInformationSymbol(sourceLineNumber)
                {
                    PropertyId = type,
                    Value = value
                });
            }
        }

        /// <summary>
        /// Ensure transform is uninstallable.
        /// </summary>
        /// <param name="productCode">Product code in transform.</param>
        /// <param name="transform">Transform generated by torch.</param>
        /// <returns>True if the transform is uninstallable</returns>
        private bool CheckUninstallableTransform(string productCode, WindowsInstallerData transform)
        {
            var success = true;

            foreach (var tableName in PatchUninstallBreakingTables)
            {
                if (transform.TryGetTable(tableName, out var table))
                {
                    foreach (var row in table.Rows)
                    {
                        if (row.Operation == RowOperation.Add)
                        {
                            success = false;

                            var primaryKey = row.GetPrimaryKey('/') ?? String.Empty;

                            this.Messaging.Write(ErrorMessages.NewRowAddedInTable(row.SourceLineNumbers, productCode, table.Name, primaryKey));
                        }
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Reduce the transform according to the patch references.
        /// </summary>
        /// <param name="transform">transform generated by torch.</param>
        /// <param name="patchRefSymbols">Table contains patch family filter.</param>
        /// <returns>true if the transform is not empty</returns>
        private bool ReduceTransform(WindowsInstallerData transform, IEnumerable<WixPatchRefSymbol> patchRefSymbols)
        {
            // identify sections to keep
            var oldSections = new Dictionary<string, Row>();
            var newSections = new Dictionary<string, Row>();
            var tableKeyRows = new Dictionary<string, Dictionary<string, Row>>();
            var sequenceList = new List<Table>();
            var componentFeatureAddsIndex = new Dictionary<string, List<string>>();
            var customActionTable = new Dictionary<string, Row>();
            var directoryTableAdds = new Dictionary<string, Row>();
            var featureTableAdds = new Dictionary<string, Row>();
            var keptComponents = new Dictionary<string, Row>();
            var keptDirectories = new Dictionary<string, Row>();
            var keptFeatures = new Dictionary<string, Row>();
            var keptLockPermissions = new HashSet<string>();
            var keptMsiLockPermissionExs = new HashSet<string>();

            var componentCreateFolderIndex = new Dictionary<string, List<string>>();
            var directoryLockPermissionsIndex = new Dictionary<string, List<Row>>();
            var directoryMsiLockPermissionsExIndex = new Dictionary<string, List<Row>>();

            foreach (var patchRefSymbol in patchRefSymbols)
            {
                var tableName = patchRefSymbol.Table;
                var key = patchRefSymbol.PrimaryKeys;

                // Short circuit filtering if all changes should be included.
                if ("*" == tableName && "*" == key)
                {
                    RemoveProductCodeFromTransform(transform);
                    return true;
                }

                if (!transform.Tables.TryGetTable(tableName, out var table))
                {
                    // Table not found.
                    continue;
                }

                // Index the table.
                if (!tableKeyRows.TryGetValue(tableName, out var keyRows))
                {
                    keyRows = new Dictionary<string, Row>();
                    tableKeyRows.Add(tableName, keyRows);

                    foreach (var newRow in table.Rows)
                    {
                        var primaryKey = newRow.GetPrimaryKey();
                        keyRows.Add(primaryKey, newRow);
                    }
                }

                if (!keyRows.TryGetValue(key, out var row))
                {
                    // Row not found.
                    continue;
                }

                // Differ.sectionDelimiter
                var sections = row.SectionId.Split('/');
                oldSections[sections[0]] = row;
                newSections[sections[1]] = row;
            }

            // throw away sections not referenced
            var keptRows = 0;
            Table directoryTable = null;
            Table featureTable = null;
            Table lockPermissionsTable = null;
            Table msiLockPermissionsTable = null;

            foreach (var table in transform.Tables)
            {
                if ("_SummaryInformation" == table.Name)
                {
                    continue;
                }

                if (table.Name == "AdminExecuteSequence"
                    || table.Name == "AdminUISequence"
                    || table.Name == "AdvtExecuteSequence"
                    || table.Name == "InstallUISequence"
                    || table.Name == "InstallExecuteSequence")
                {
                    sequenceList.Add(table);
                    continue;
                }

                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];

                    if (table.Name == "CreateFolder")
                    {
                        var createFolderComponentId = row.FieldAsString(1);

                        if (!componentCreateFolderIndex.TryGetValue(createFolderComponentId, out var directoryList))
                        {
                            directoryList = new List<string>();
                            componentCreateFolderIndex.Add(createFolderComponentId, directoryList);
                        }

                        directoryList.Add(row.FieldAsString(0));
                    }

                    if (table.Name == "CustomAction")
                    {
                        customActionTable.Add(row.FieldAsString(0), row);
                    }

                    if (table.Name == "Directory")
                    {
                        directoryTable = table;
                        if (RowOperation.Add == row.Operation)
                        {
                            directoryTableAdds.Add(row.FieldAsString(0), row);
                        }
                    }

                    if (table.Name == "Feature")
                    {
                        featureTable = table;
                        if (RowOperation.Add == row.Operation)
                        {
                            featureTableAdds.Add(row.FieldAsString(0), row);
                        }
                    }

                    if (table.Name == "FeatureComponents")
                    {
                        if (RowOperation.Add == row.Operation)
                        {
                            var featureId = row.FieldAsString(0);
                            var componentId = row.FieldAsString(1);

                            if (!componentFeatureAddsIndex.TryGetValue(componentId, out var featureList))
                            {
                                featureList = new List<string>();
                                componentFeatureAddsIndex.Add(componentId, featureList);
                            }

                            featureList.Add(featureId);
                        }
                    }

                    if (table.Name == "LockPermissions")
                    {
                        lockPermissionsTable = table;
                        if ("CreateFolder" == row.FieldAsString(1))
                        {
                            var directoryId = row.FieldAsString(0);

                            if (!directoryLockPermissionsIndex.TryGetValue(directoryId, out var rowList))
                            {
                                rowList = new List<Row>();
                                directoryLockPermissionsIndex.Add(directoryId, rowList);
                            }

                            rowList.Add(row);
                        }
                    }

                    if (table.Name == "MsiLockPermissionsEx")
                    {
                        msiLockPermissionsTable = table;
                        if ("CreateFolder" == row.FieldAsString(1))
                        {
                            var directoryId = row.FieldAsString(0);

                            if (!directoryMsiLockPermissionsExIndex.TryGetValue(directoryId, out var rowList))
                            {
                                rowList = new List<Row>();
                                directoryMsiLockPermissionsExIndex.Add(directoryId, rowList);
                            }

                            rowList.Add(row);
                        }
                    }

                    if (null == row.SectionId)
                    {
                        table.Rows.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        var sections = row.SectionId.Split('/');
                        // ignore the row without section id.
                        if (0 == sections[0].Length && 0 == sections[1].Length)
                        {
                            table.Rows.RemoveAt(i);
                            i--;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            if ("Component" == table.Name)
                            {
                                keptComponents.Add(row.FieldAsString(0), row);
                            }

                            if ("Directory" == table.Name)
                            {
                                keptDirectories.Add(row.FieldAsString(0), row);
                            }

                            if ("Feature" == table.Name)
                            {
                                keptFeatures.Add(row.FieldAsString(0), row);
                            }

                            keptRows++;
                        }
                        else
                        {
                            table.Rows.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            keptRows += ReduceTransformSequenceTable(sequenceList, oldSections, newSections, customActionTable);

            if (null != directoryTable)
            {
                foreach (var componentRow in keptComponents.Values)
                {
                    var componentId = componentRow.FieldAsString(0);

                    if (RowOperation.Add == componentRow.Operation)
                    {
                        // Make sure each added component has its required directory and feature heirarchy.
                        var directoryId = componentRow.FieldAsString(2);
                        while (null != directoryId && directoryTableAdds.TryGetValue(directoryId, out var directoryRow))
                        {
                            if (!keptDirectories.ContainsKey(directoryId))
                            {
                                directoryTable.Rows.Add(directoryRow);
                                keptDirectories.Add(directoryId, directoryRow);
                                keptRows++;
                            }

                            directoryId = directoryRow.FieldAsString(1);
                        }

                        if (componentFeatureAddsIndex.TryGetValue(componentId, out var componentFeatureIds))
                        {
                            foreach (var featureId in componentFeatureIds)
                            {
                                var currentFeatureId = featureId;
                                while (null != currentFeatureId && featureTableAdds.TryGetValue(currentFeatureId, out var featureRow))
                                {
                                    if (!keptFeatures.ContainsKey(currentFeatureId))
                                    {
                                        featureTable.Rows.Add(featureRow);
                                        keptFeatures.Add(currentFeatureId, featureRow);
                                        keptRows++;
                                    }

                                    currentFeatureId = featureRow.FieldAsString(1);
                                }
                            }
                        }
                    }

                    // Hook in changes LockPermissions and MsiLockPermissions for folders for each component that has been kept.
                    foreach (var keptComponentId in keptComponents.Keys)
                    {
                        if (componentCreateFolderIndex.TryGetValue(keptComponentId, out var directoryList))
                        {
                            foreach (var directoryId in directoryList)
                            {
                                if (directoryLockPermissionsIndex.TryGetValue(directoryId, out var lockPermissionsRowList))
                                {
                                    foreach (var lockPermissionsRow in lockPermissionsRowList)
                                    {
                                        var key = lockPermissionsRow.GetPrimaryKey('/');
                                        if (keptLockPermissions.Add(key))
                                        {
                                            lockPermissionsTable.Rows.Add(lockPermissionsRow);
                                            keptRows++;
                                        }
                                    }
                                }

                                if (directoryMsiLockPermissionsExIndex.TryGetValue(directoryId, out var msiLockPermissionsExRowList))
                                {
                                    foreach (var msiLockPermissionsExRow in msiLockPermissionsExRowList)
                                    {
                                        var key = msiLockPermissionsExRow.GetPrimaryKey('/');
                                        if (keptMsiLockPermissionExs.Add(key))
                                        {
                                            msiLockPermissionsTable.Rows.Add(msiLockPermissionsExRow);
                                            keptRows++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            keptRows += ReduceTransformSequenceTable(sequenceList, oldSections, newSections, customActionTable);

            // Delete tables that are empty.
            var tablesToDelete = transform.Tables.Where(t => t.Rows.Count == 0).Select(t => t.Name);

            foreach (var tableName in tablesToDelete)
            {
                transform.Tables.Remove(tableName);
            }

            return keptRows > 0;
        }

        private void Validate(PatchTransform patchTransform)
        {
            var transformPath = patchTransform.Baseline; // TODO: this is used in error messages, how best to set it?
            var transform = patchTransform.Transform;

            // Changing the ProdocutCode in a patch transform is not recommended.
            if (transform.TryGetTable("Property", out var propertyTable))
            {
                foreach (var row in propertyTable.Rows)
                {
                    // Only interested in modified rows; fast check.
                    if (RowOperation.Modify == row.Operation &&
                        "ProductCode".Equals(row.FieldAsString(0), StringComparison.Ordinal))
                    {
                        this.Messaging.Write(WarningMessages.MajorUpgradePatchNotRecommended());
                    }
                }
            }

            // If there is nothing in the component table we can return early because the remaining checks are component based.
            if (!transform.TryGetTable("Component", out var componentTable))
            {
                return;
            }

            // Index Feature table row operations
            var featureOps = new Dictionary<string, RowOperation>();
            if (transform.TryGetTable("Feature", out var featureTable))
            {
                foreach (var row in featureTable.Rows)
                {
                    featureOps[row.FieldAsString(0)] = row.Operation;
                }
            }

            // Index Component table and check for keypath modifications
            var componentKeyPath = new Dictionary<string, string>();
            var deletedComponent = new Dictionary<string, Row>();
            foreach (var row in componentTable.Rows)
            {
                var id = row.FieldAsString(0);
                var keypath = row.FieldAsString(5) ?? String.Empty;

                componentKeyPath.Add(id, keypath);

                if (RowOperation.Delete == row.Operation)
                {
                    deletedComponent.Add(id, row);
                }
                else if (RowOperation.Modify == row.Operation)
                {
                    if (row.Fields[1].Modified)
                    {
                        // Changing the guid of a component is equal to deleting the old one and adding a new one.
                        deletedComponent.Add(id, row);
                    }

                    // If the keypath is modified its an error
                    if (row.Fields[5].Modified)
                    {
                        this.Messaging.Write(ErrorMessages.InvalidKeypathChange(row.SourceLineNumbers, id, transformPath));
                    }
                }
            }

            // Verify changes in the file table
            if (transform.TryGetTable("File", out var fileTable))
            {
                var componentWithChangedKeyPath = new Dictionary<string, string>();
                foreach (FileRow row in fileTable.Rows)
                {
                    if (RowOperation.None == row.Operation)
                    {
                        continue;
                    }

                    var fileId = row.File;
                    var componentId = row.Component;

                    // If this file is the keypath of a component
                    if (componentKeyPath.TryGetValue(componentId, out var keyPath) && keyPath.Equals(fileId, StringComparison.Ordinal))
                    {
                        if (row.Fields[2].Modified)
                        {
                            // You can't change the filename of a file that is the keypath of a component.
                            this.Messaging.Write(ErrorMessages.InvalidKeypathChange(row.SourceLineNumbers, componentId, transformPath));
                        }

                        if (!componentWithChangedKeyPath.ContainsKey(componentId))
                        {
                            componentWithChangedKeyPath.Add(componentId, fileId);
                        }
                    }

                    if (RowOperation.Delete == row.Operation)
                    {
                        // If the file is removed from a component that is not deleted.
                        if (!deletedComponent.ContainsKey(componentId))
                        {
                            var foundRemoveFileEntry = false;
                            var filename = Common.GetName(row.FieldAsString(2), false, true);

                            if (transform.TryGetTable("RemoveFile", out var removeFileTable))
                            {
                                foreach (var removeFileRow in removeFileTable.Rows)
                                {
                                    if (RowOperation.Delete == removeFileRow.Operation)
                                    {
                                        continue;
                                    }

                                    if (componentId == removeFileRow.FieldAsString(1))
                                    {
                                        // Check if there is a RemoveFile entry for this file
                                        if (null != removeFileRow[2])
                                        {
                                            var removeFileName = Common.GetName(removeFileRow.FieldAsString(2), false, true);

                                            // Convert the MSI format for a wildcard string to Regex format.
                                            removeFileName = removeFileName.Replace('.', '|').Replace('?', '.').Replace("*", ".*").Replace("|", "\\.");

                                            var regex = new Regex(removeFileName, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                                            if (regex.IsMatch(filename))
                                            {
                                                foundRemoveFileEntry = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!foundRemoveFileEntry)
                            {
                                this.Messaging.Write(WarningMessages.InvalidRemoveFile(row.SourceLineNumbers, fileId, componentId));
                            }
                        }
                    }
                }
            }

            var featureComponentsTable = transform.Tables["FeatureComponents"];

            if (0 < deletedComponent.Count)
            {
                // Index FeatureComponents table.
                var featureComponents = new Dictionary<string, List<string>>();

                if (null != featureComponentsTable)
                {
                    foreach (var row in featureComponentsTable.Rows)
                    {
                        var componentId = row.FieldAsString(1);

                        if (!featureComponents.TryGetValue(componentId, out var features))
                        {
                            features = new List<string>();
                            featureComponents.Add(componentId, features);
                        }

                        features.Add(row.FieldAsString(0));
                    }
                }

                // Check to make sure if a component was deleted, the feature was too.
                foreach (var entry in deletedComponent)
                {
                    if (featureComponents.TryGetValue(entry.Key, out var features))
                    {
                        foreach (var featureId in features)
                        {
                            if (!featureOps.TryGetValue(featureId, out var op) || op != RowOperation.Delete)
                            {
                                // The feature was not deleted.
                                this.Messaging.Write(ErrorMessages.InvalidRemoveComponent(((Row)entry.Value).SourceLineNumbers, entry.Key.ToString(), featureId, transformPath));
                            }
                        }
                    }
                }
            }

            // Warn if new components are added to existing features
            if (null != featureComponentsTable)
            {
                foreach (var row in featureComponentsTable.Rows)
                {
                    if (RowOperation.Add == row.Operation)
                    {
                        // Check if the feature is in the Feature table
                        var feature_ = row.FieldAsString(0);
                        var component_ = row.FieldAsString(1);

                        // Features may not be present if not referenced
                        if (!featureOps.ContainsKey(feature_) || RowOperation.Add != (RowOperation)featureOps[feature_])
                        {
                            this.Messaging.Write(WarningMessages.NewComponentAddedToExistingFeature(row.SourceLineNumbers, component_, feature_, transformPath));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove the ProductCode property from the transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <remarks>
        /// Changing the ProductCode is not supported in a patch.
        /// </remarks>
        private static void RemoveProductCodeFromTransform(WindowsInstallerData transform)
        {
            if (transform.Tables.TryGetTable("Property", out var propertyTable))
            {
                for (var i = 0; i < propertyTable.Rows.Count; ++i)
                {
                    var propertyRow = propertyTable.Rows[i];
                    var property = (string)propertyRow[0];

                    if ("ProductCode" == property)
                    {
                        propertyTable.Rows.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check if the section is in a PatchFamily.
        /// </summary>
        /// <param name="oldSection">Section id in target wixout</param>
        /// <param name="newSection">Section id in upgrade wixout</param>
        /// <param name="oldSections">Dictionary contains section id should be kept in the baseline wixout.</param>
        /// <param name="newSections">Dictionary contains section id should be kept in the upgrade wixout.</param>
        /// <returns>true if section in patch family</returns>
        private static bool IsInPatchFamily(string oldSection, string newSection, Dictionary<string, Row> oldSections, Dictionary<string, Row> newSections)
        {
            var result = false;

            if ((String.IsNullOrEmpty(oldSection) && newSections.ContainsKey(newSection)) || (String.IsNullOrEmpty(newSection) && oldSections.ContainsKey(oldSection)))
            {
                result = true;
            }
            else if (!String.IsNullOrEmpty(oldSection) && !String.IsNullOrEmpty(newSection) && (oldSections.ContainsKey(oldSection) || newSections.ContainsKey(newSection)))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Reduce the transform sequence tables.
        /// </summary>
        /// <param name="sequenceList">ArrayList of tables to be reduced</param>
        /// <param name="oldSections">Hashtable contains section id should be kept in the baseline wixout.</param>
        /// <param name="newSections">Hashtable contains section id should be kept in the target wixout.</param>
        /// <param name="customAction">Hashtable contains all the rows in the CustomAction table.</param>
        /// <returns>Number of rows left</returns>
        private static int ReduceTransformSequenceTable(List<Table> sequenceList, Dictionary<string, Row> oldSections, Dictionary<string, Row> newSections, Dictionary<string, Row> customAction)
        {
            var keptRows = 0;

            foreach (var currentTable in sequenceList)
            {
                for (var i = 0; i < currentTable.Rows.Count; i++)
                {
                    var row = currentTable.Rows[i];
                    var actionName = row.Fields[0].Data.ToString();
                    var sections = row.SectionId.Split('/');
                    var isSectionIdEmpty = (sections[0].Length == 0 && sections[1].Length == 0);

                    if (row.Operation == RowOperation.None)
                    {
                        // Ignore the rows without section id.
                        if (isSectionIdEmpty)
                        {
                            currentTable.Rows.RemoveAt(i);
                            i--;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            currentTable.Rows.RemoveAt(i);
                            i--;
                        }
                    }
                    else if (row.Operation == RowOperation.Modify)
                    {
                        var sequenceChanged = row.Fields[2].Modified;
                        var conditionChanged = row.Fields[1].Modified;

                        if (sequenceChanged && !conditionChanged)
                        {
                            keptRows++;
                        }
                        else if (!sequenceChanged && conditionChanged)
                        {
                            if (isSectionIdEmpty)
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                            {
                                keptRows++;
                            }
                            else
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (sequenceChanged && conditionChanged)
                        {
                            if (isSectionIdEmpty)
                            {
                                row.Fields[1].Modified = false;
                                keptRows++;
                            }
                            else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                            {
                                keptRows++;
                            }
                            else
                            {
                                row.Fields[1].Modified = false;
                                keptRows++;
                            }
                        }
                    }
                    else if (row.Operation == RowOperation.Delete)
                    {
                        if (isSectionIdEmpty)
                        {
                            // it is a stardard action which is added by wix, we should keep this action.
                            row.Operation = RowOperation.None;
                            keptRows++;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            if (customAction.ContainsKey(actionName))
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                // it is a stardard action, we should keep this action.
                                row.Operation = RowOperation.None;
                                keptRows++;
                            }
                        }
                    }
                    else if (row.Operation == RowOperation.Add)
                    {
                        if (isSectionIdEmpty)
                        {
                            keptRows++;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            if (customAction.ContainsKey(actionName))
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                keptRows++;
                            }
                        }
                    }
                }
            }

            return keptRows;
        }

        /// <summary>
        /// Create the #transform for the given main transform.
        /// </summary>
        private WindowsInstallerData BuildPairedTransform(Dictionary<SummaryInformationType, SummaryInformationSymbol> summaryInfo, Dictionary<string, MsiPatchMetadataSymbol> patchMetadata, WixPatchIdSymbol patchIdSymbol, WindowsInstallerData mainTransform, MediaSymbol mediaSymbol, WixPatchBaselineSymbol baselineSymbol, out string productCode)
        {
            productCode = null;

            var pairedTransform = new WindowsInstallerData(null)
            {
                Type = OutputType.Transform,
                Codepage = mainTransform.Codepage
            };

            // lookup productVersion property to correct summaryInformation
            var newProductVersion = mainTransform.Tables["Property"]?.Rows.FirstOrDefault(r => r.FieldAsString(0) == "ProductVersion")?.FieldAsString(1);

            var mainSummaryTable = mainTransform.Tables["_SummaryInformation"];
            var mainSummaryRows = mainSummaryTable.Rows.ToDictionary(r => r.FieldAsInteger(0));

            var baselineValidationFlags = ((int)baselineSymbol.ValidationFlags).ToString(CultureInfo.InvariantCulture);

            if (!mainSummaryRows.ContainsKey((int)SummaryInformationType.TransformValidationFlags))
            {
                var mainSummaryRow = mainSummaryTable.CreateRow(baselineSymbol.SourceLineNumbers);
                mainSummaryRow[0] = (int)SummaryInformationType.TransformValidationFlags;
                mainSummaryRow[1] = baselineValidationFlags;
            }

            // copy summary information from core transform
            var pairedSummaryTable = pairedTransform.EnsureTable(this.tableDefinitions["_SummaryInformation"]);

            foreach (var mainSummaryRow in mainSummaryTable.Rows)
            {
                var type = (SummaryInformationType)mainSummaryRow.FieldAsInteger(0);
                var value = mainSummaryRow.FieldAsString(1);
                switch (type)
                {
                    case SummaryInformationType.TransformProductCodes:
                        var propertyData = value.Split(';');
                        var oldProductVersion = propertyData[0].Substring(38);
                        var upgradeCode = propertyData[2];
                        productCode = propertyData[0].Substring(0, 38);

                        if (newProductVersion == null)
                        {
                            newProductVersion = oldProductVersion;
                        }

                        // Force mainTranform to 'old;new;upgrade' and pairedTransform to 'new;new;upgrade'
                        mainSummaryRow[1] = String.Concat(productCode, oldProductVersion, ';', productCode, newProductVersion, ';', upgradeCode);
                        value = String.Concat(productCode, newProductVersion, ';', productCode, newProductVersion, ';', upgradeCode);
                        break;
                    case SummaryInformationType.TransformValidationFlags: // use validation flags authored into the patch XML.
                        value = baselineValidationFlags;
                        mainSummaryRow[1] = value;
                        break;
                }

                var pairedSummaryRow = pairedSummaryTable.CreateRow(mainSummaryRow.SourceLineNumbers);
                pairedSummaryRow[0] = mainSummaryRow[0];
                pairedSummaryRow[1] = value;
            }

            if (productCode == null)
            {
                this.Messaging.Write(ErrorMessages.CouldNotDetermineProductCodeFromTransformSummaryInfo());
                return null;
            }

            // Copy File table
            if (mainTransform.Tables.TryGetTable("File", out var mainFileTable) && 0 < mainFileTable.Rows.Count)
            {
                var pairedFileTable = pairedTransform.EnsureTable(mainFileTable.Definition);

                foreach (FileRow mainFileRow in mainFileTable.Rows)
                {
                    // Set File.Sequence to non null to satisfy transform bind.
                    mainFileRow.Sequence = 1;

                    // Delete's don't need rows in the paired transform.
                    if (mainFileRow.Operation == RowOperation.Delete)
                    {
                        continue;
                    }

                    var pairedFileRow = (FileRow)pairedFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                    pairedFileRow.Operation = RowOperation.Modify;
                    mainFileRow.CopyTo(pairedFileRow);

                    // Override authored media for patch bind.
                    mainFileRow.DiskId = mediaSymbol.DiskId;

                    // Suppress any change to File.Sequence to avoid bloat.
                    mainFileRow.Fields[7].Modified = false;

                    // Force File row to appear in the transform.
                    switch (mainFileRow.Operation)
                    {
                        case RowOperation.Modify:
                        case RowOperation.Add:
                            pairedFileRow.Attributes |= WindowsInstallerConstants.MsidbFileAttributesPatchAdded;
                            pairedFileRow.Fields[6].Modified = true;
                            pairedFileRow.Operation = mainFileRow.Operation;
                            break;
                        default:
                            pairedFileRow.Fields[6].Modified = false;
                            break;
                    }
                }
            }

            // Add Media row to pairedTransform
            var pairedMediaTable = pairedTransform.EnsureTable(this.tableDefinitions["Media"]);
            var pairedMediaRow = (MediaRow)pairedMediaTable.CreateRow(mediaSymbol.SourceLineNumbers);
            pairedMediaRow.Operation = RowOperation.Add;
            pairedMediaRow.DiskId = mediaSymbol.DiskId;
            pairedMediaRow.LastSequence = mediaSymbol.LastSequence ?? 0;
            pairedMediaRow.DiskPrompt = mediaSymbol.DiskPrompt;
            pairedMediaRow.Cabinet = mediaSymbol.Cabinet;
            pairedMediaRow.VolumeLabel = mediaSymbol.VolumeLabel;
            pairedMediaRow.Source = mediaSymbol.Source;

            // Add PatchPackage for this Media
            var pairedPackageTable = pairedTransform.EnsureTable(this.tableDefinitions["PatchPackage"]);
            pairedPackageTable.Operation = TableOperation.Add;
            var pairedPackageRow = pairedPackageTable.CreateRow(mediaSymbol.SourceLineNumbers);
            pairedPackageRow.Operation = RowOperation.Add;
            pairedPackageRow[0] = patchIdSymbol.Id.Id;
            pairedPackageRow[1] = mediaSymbol.DiskId;

            // Add the property to the patch transform's Property table.
            var pairedPropertyTable = pairedTransform.EnsureTable(this.tableDefinitions["Property"]);
            pairedPropertyTable.Operation = TableOperation.Add;

            // Add property to both identify client patches and whether those patches are removable or not
            patchMetadata.TryGetValue("AllowRemoval", out var allowRemovalSymbol);

            var pairedPropertyRow = pairedPropertyTable.CreateRow(allowRemovalSymbol?.SourceLineNumbers);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = String.Concat(patchIdSymbol.ClientPatchId, ".AllowRemoval");
            pairedPropertyRow[1] = allowRemovalSymbol?.Value ?? "0";

            // Add this patch code GUID to the patch transform to identify
            // which patches are installed, including in multi-patch
            // installations.
            pairedPropertyRow = pairedPropertyTable.CreateRow(patchIdSymbol.SourceLineNumbers);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = String.Concat(patchIdSymbol.ClientPatchId, ".PatchCode");
            pairedPropertyRow[1] = patchIdSymbol.Id.Id;

            // Add PATCHNEWPACKAGECODE to apply to admin layouts.
            pairedPropertyRow = pairedPropertyTable.CreateRow(patchIdSymbol.SourceLineNumbers);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = "PATCHNEWPACKAGECODE";
            pairedPropertyRow[1] = patchIdSymbol.Id.Id;

            // Add PATCHNEWSUMMARYCOMMENTS and PATCHNEWSUMMARYSUBJECT to apply to admin layouts.
            if (summaryInfo.TryGetValue(SummaryInformationType.Subject, out var subjectSymbol))
            {
                pairedPropertyRow = pairedPropertyTable.CreateRow(subjectSymbol.SourceLineNumbers);
                pairedPropertyRow.Operation = RowOperation.Add;
                pairedPropertyRow[0] = "PATCHNEWSUMMARYSUBJECT";
                pairedPropertyRow[1] = subjectSymbol.Value;
            }

            if (summaryInfo.TryGetValue(SummaryInformationType.Comments, out var commentsSymbol))
            {
                pairedPropertyRow = pairedPropertyTable.CreateRow(commentsSymbol.SourceLineNumbers);
                pairedPropertyRow.Operation = RowOperation.Add;
                pairedPropertyRow[0] = "PATCHNEWSUMMARYCOMMENTS";
                pairedPropertyRow[1] = commentsSymbol.Value;
            }

            return pairedTransform;
        }

        private static SortedSet<string> FinalizePatchProductCodes(List<IntermediateSymbol> symbols, SortedSet<string> productCodes)
        {
            var patchTargetSymbols = symbols.OfType<WixPatchTargetSymbol>().ToList();

            if (patchTargetSymbols.Any())
            {
                var targets = new SortedSet<string>();
                var replace = true;
                foreach (var wixPatchTargetRow in patchTargetSymbols)
                {
                    var target = wixPatchTargetRow.ProductCode.ToUpperInvariant();
                    if (target == "*")
                    {
                        replace = false;
                    }
                    else
                    {
                        targets.Add(target);
                    }
                }

                // Replace the target ProductCodes with the authored list.
                if (replace)
                {
                    productCodes = targets;
                }
                else
                {
                    // Copy the authored target ProductCodes into the list.
                    foreach (var target in targets)
                    {
                        productCodes.Add(target);
                    }
                }
            }

            return productCodes;
        }
    }
}
