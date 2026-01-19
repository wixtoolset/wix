// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class UpdateTransformsWithFileFacades
    {
        public UpdateTransformsWithFileFacades(IMessaging messaging, IntermediateSection section, IEnumerable<SubStorage> subStorages, TableDefinitionCollection tableDefinitions, IEnumerable<IFileFacade> fileFacades)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.SubStorages = subStorages;
            this.TableDefinitions = tableDefinitions;
            this.FileFacades = fileFacades;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<SubStorage> SubStorages { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private IEnumerable<IFileFacade> FileFacades { get; }

        public void Execute()
        {
            var fileFacadesByDiskId = this.IndexFileFacadesByDiskId();

            var mediaSymbolsByDiskId = this.Section.Symbols.OfType<MediaSymbol>().ToDictionary(m => m.DiskId);

            // Index paired transforms by name without the "#" prefix.
            var pairedTransforms = this.SubStorages.Where(s => s.Name.StartsWith(PatchConstants.PairedPatchTransformPrefix)).ToDictionary(s => s.Name, s => s.Data);

            foreach (var substorage in this.SubStorages.Where(s => !s.Name.StartsWith(PatchConstants.PairedPatchTransformPrefix)))
            {
                var mainTransform = substorage.Data;

                var pairedTransform = pairedTransforms[PatchConstants.PairedPatchTransformPrefix + substorage.Name];

                // Update the Media.LastSequence in the paired transforms.
                foreach (var pairedMediaRow in pairedTransform.Tables["Media"].Rows.Cast<MediaRow>())
                {
                    if (mediaSymbolsByDiskId.TryGetValue(pairedMediaRow.DiskId, out var mediaSymbol) && mediaSymbol.LastSequence.HasValue)
                    {
                        pairedMediaRow.LastSequence = mediaSymbol.LastSequence.Value;
                    }
                    else // TODO: This shouldn't be possible.
                    {
                        throw new InvalidDataException();
                    }
                }

                // Validate file row changes for keypath-related issues
                this.ValidateFileRowChanges(mainTransform);

                // Copy File bind data into transforms
                if (mainTransform.Tables.TryGetTable("File", out var mainFileTable))
                {
                    // Index File table of pairedTransform
                    var pairedFileRows = new RowDictionary<FileRow>(pairedTransform.Tables["File"]);

                    var mainMsiFileHashIndex = new RowDictionary<Row>(mainTransform.Tables["MsiFileHash"]);

                    // Remove the MsiFileHash table because it will be updated later with the final file hash for each file
                    mainTransform.Tables.Remove("MsiFileHash");

                    foreach (var mainFileRow in mainFileTable.Rows.Where(r => r.Operation == RowOperation.Add || r.Operation == RowOperation.Modify).Cast<FileRow>())
                    {
                        // TODO: Wasn't this indexing done at the top of this method?
                        // Index main transform files by diskId+fileId
                        if (!fileFacadesByDiskId.TryGetValue(mainFileRow.DiskId, out var mediaFacades))
                        {
                            mediaFacades = new Dictionary<string, IFileFacade>();
                            fileFacadesByDiskId.Add(mainFileRow.DiskId, mediaFacades);
                        }

                        // Copy data from the facade back to the appropriate transform.
                        if (mediaFacades.TryGetValue(mainFileRow.File, out var facade))
                        {
                            var pairedFileRow = pairedFileRows.Get(mainFileRow.File);

                            TryModifyField(mainFileRow, 3, facade.FileSize);

                            TryModifyField(mainFileRow, 4, facade.Version);

                            TryModifyField(mainFileRow, 5, facade.Language);

#if TODO_PATCHING_DELTA
                            // File.Attribute should not change for binary deltas, otherwise copy File Attributes from main transform row.
                            if (null != facade.Patch)
#endif
                            {
                                TryModifyField(pairedFileRow, 6, mainFileRow.Attributes);
                                mainFileRow.Fields[6].Modified = false;
                            }

#if TODO_PATCHING_DELTA
                            // File.Sequence is updated in Patch table instead of File table for delta patches
                            if (null != facade.Patch)
                            {
                                pairedFileRow.Fields[7].Modified = false;
                            }
                            else
#endif
                            {
                                // File.Sequence is updated in pairedTransform, not mainTransform.
                                TryModifyField(pairedFileRow, 7, facade.Sequence);
                            }
                            mainFileRow.Fields[7].Modified = false;

                            this.ProcessMsiFileHash(mainTransform, mainFileRow, facade.MsiFileHashSymbol, mainMsiFileHashIndex);

                            this.ProcessMsiAssemblyName(mainTransform, mainFileRow, facade);

#if TODO_PATCHING_DELTA
                            // Add patch header for this file
                            if (null != facade.Patch)
                            {
                                // Add the PatchFiles action automatically to the AdminExecuteSequence and InstallExecuteSequence tables.
                                this.AddPatchFilesActionToSequenceTable(SequenceTable.AdminExecuteSequence, mainTransform, pairedTransform, mainFileRow);
                                this.AddPatchFilesActionToSequenceTable(SequenceTable.InstallExecuteSequence, mainTransform, pairedTransform, mainFileRow);

                                // Add to Patch table
                                var patchTable = pairedTransform.EnsureTable(this.TableDefinitions["Patch"]);
                                if (0 == patchTable.Rows.Count)
                                {
                                    patchTable.Operation = TableOperation.Add;
                                }

                                var patchRow = patchTable.CreateRow(mainFileRow.SourceLineNumbers);
                                patchRow[0] = facade.File;
                                patchRow[1] = facade.Sequence;

                                var patchFile = new FileInfo(facade.Source);
                                patchRow[2] = (int)patchFile.Length;
                                patchRow[3] = 0 == (PatchAttributeType.AllowIgnoreOnError & facade.PatchAttributes) ? 0 : 1;

                                var streamName = patchTable.Name + "." + patchRow[0] + "." + patchRow[1];
                                if (Msi.MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                {
                                    streamName = "_" + Guid.NewGuid().ToString("D").ToUpperInvariant().Replace('-', '_');

                                    var patchHeadersTable = pairedTransform.EnsureTable(this.TableDefinitions["MsiPatchHeaders"]);
                                    if (0 == patchHeadersTable.Rows.Count)
                                    {
                                        patchHeadersTable.Operation = TableOperation.Add;
                                    }

                                    var patchHeadersRow = patchHeadersTable.CreateRow(mainFileRow.SourceLineNumbers);
                                    patchHeadersRow[0] = streamName;
                                    patchHeadersRow[1] = facade.Patch;
                                    patchRow[5] = streamName;
                                    patchHeadersRow.Operation = RowOperation.Add;
                                }
                                else
                                {
                                    patchRow[4] = facade.Patch;
                                }
                                patchRow.Operation = RowOperation.Add;
                            }
#endif
                        }
                        else
                        {
                            // TODO: throw because all transform rows should have made it into the patch
                        }
                    }
                }
            }
        }

        private void ProcessMsiFileHash(WindowsInstallerData transform, FileRow fileRow, MsiFileHashSymbol msiFileHashSymbol, RowDictionary<Row> msiFileHashIndex)
        {
            Row msiFileHashRow = null;

            if (msiFileHashSymbol != null || msiFileHashIndex.TryGetValue(fileRow.File, out msiFileHashRow))
            {
                var sourceLineNumbers = msiFileHashSymbol?.SourceLineNumbers ?? msiFileHashRow?.SourceLineNumbers;

                var transformHashTable = transform.EnsureTable(this.TableDefinitions["MsiFileHash"]);

                var transformHashRow = transformHashTable.CreateRow(sourceLineNumbers);
                transformHashRow.Operation = fileRow.Operation; // Assume the MsiFileHash operation follows the File one.

                transformHashRow[0] = fileRow.File;
                transformHashRow[1] = msiFileHashSymbol?.Options ?? msiFileHashRow?.Fields[1].Data;

                // Assume all hash fields have been modified.
                TryModifyField(transformHashRow, 2, msiFileHashSymbol?.HashPart1 ?? msiFileHashRow?.Fields[2].Data);
                TryModifyField(transformHashRow, 3, msiFileHashSymbol?.HashPart2 ?? msiFileHashRow?.Fields[3].Data);
                TryModifyField(transformHashRow, 4, msiFileHashSymbol?.HashPart3 ?? msiFileHashRow?.Fields[4].Data);
                TryModifyField(transformHashRow, 5, msiFileHashSymbol?.HashPart4 ?? msiFileHashRow?.Fields[5].Data);
            }
        }

        private void ProcessMsiAssemblyName(WindowsInstallerData transform, FileRow fileRow, IFileFacade facade)
        {
            if (facade.AssemblyNameSymbols.Count > 0)
            {
                var assemblyNameTable = transform.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);

                foreach (var assemblyNameSymbol in facade.AssemblyNameSymbols)
                {
                    // Copy if there isn't an identical modified/added row already in the transform.
                    var foundMatchingModifiedRow = false;
                    foreach (var mainAssemblyNameRow in assemblyNameTable.Rows.Where(r => r.Operation != RowOperation.None))
                    {
                        var component = mainAssemblyNameRow.FieldAsString(0);
                        var name = mainAssemblyNameRow.FieldAsString(1);

                        if (assemblyNameSymbol.ComponentRef == component && assemblyNameSymbol.Name == name)
                        {
                            foundMatchingModifiedRow = true;
                            break;
                        }
                    }

                    if (!foundMatchingModifiedRow)
                    {
                        var assemblyNameRow = assemblyNameTable.CreateRow(fileRow.SourceLineNumbers);
                        assemblyNameRow[0] = assemblyNameSymbol.ComponentRef;
                        assemblyNameRow[1] = assemblyNameSymbol.Name;
                        assemblyNameRow[2] = assemblyNameSymbol.Value;

                        // assume value field has been modified
                        assemblyNameRow.Fields[2].Modified = true;
                        assemblyNameRow.Operation = fileRow.Operation;
                    }
                }
            }
        }

        private Dictionary<int, Dictionary<string, IFileFacade>> IndexFileFacadesByDiskId()
        {
            var fileFacadesByDiskId = new Dictionary<int, Dictionary<string, IFileFacade>>();

            // Index patch file facades by diskId+fileId.
            foreach (var facade in this.FileFacades)
            {
                if (!fileFacadesByDiskId.TryGetValue(facade.DiskId, out var mediaFacades))
                {
                    mediaFacades = new Dictionary<string, IFileFacade>();
                    fileFacadesByDiskId.Add(facade.DiskId, mediaFacades);
                }

                mediaFacades.Add(facade.Id, facade);
            }

            return fileFacadesByDiskId;
        }

        /// <summary>
        /// Adds the PatchFiles action to the sequence table if it does not already exist.
        /// </summary>
        /// <param name="table">The sequence table to check or modify.</param>
        /// <param name="mainTransform">The primary authoring transform.</param>
        /// <param name="pairedTransform">The secondary patch transform.</param>
        /// <param name="mainFileRow">The file row that contains information about the patched file.</param>
        private void AddPatchFilesActionToSequenceTable(SequenceTable table, WindowsInstallerData mainTransform, WindowsInstallerData pairedTransform, Row mainFileRow)
        {
            var tableName = table.ToString();

            // Find/add PatchFiles action (also determine sequence for it).
            // Search mainTransform first, then pairedTransform (pairedTransform overrides).
            var hasPatchFilesAction = false;
            var installFilesSequence = 0;
            var duplicateFilesSequence = 0;

            TestSequenceTableForPatchFilesAction(
                    mainTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref installFilesSequence,
                    ref duplicateFilesSequence);
            TestSequenceTableForPatchFilesAction(
                    pairedTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref installFilesSequence,
                    ref duplicateFilesSequence);
            if (!hasPatchFilesAction)
            {
                WindowsInstallerStandard.TryGetStandardAction(tableName, "PatchFiles", out var patchFilesActionSymbol);

                var sequence = patchFilesActionSymbol.Sequence;

                // Test for default sequence value's appropriateness
                if (installFilesSequence >= sequence || (0 != duplicateFilesSequence && duplicateFilesSequence <= sequence))
                {
                    if (0 != duplicateFilesSequence)
                    {
                        if (duplicateFilesSequence < installFilesSequence)
                        {
                            throw new WixException(WindowsInstallerBackendErrors.InsertInvalidSequenceActionOrder(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionSymbol.Action));
                        }
                        else
                        {
                            sequence = (duplicateFilesSequence + installFilesSequence) / 2;
                            if (installFilesSequence == sequence || duplicateFilesSequence == sequence)
                            {
                                throw new WixException(WindowsInstallerBackendErrors.InsertSequenceNoSpace(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionSymbol.Action));
                            }
                        }
                    }
                    else
                    {
                        sequence = installFilesSequence + 1;
                    }
                }

                var sequenceTable = pairedTransform.EnsureTable(this.TableDefinitions[tableName]);
                if (0 == sequenceTable.Rows.Count)
                {
                    sequenceTable.Operation = TableOperation.Add;
                }

                var patchAction = sequenceTable.CreateRow(null);
                patchAction[0] = patchFilesActionSymbol.Action;
                patchAction[1] = patchFilesActionSymbol.Condition;
                patchAction[2] = sequence;
                patchAction.Operation = RowOperation.Add;
            }
        }

        private static bool TryModifyField(Row row, int index, object value)
        {
            var field = row.Fields[index];

            if (field.Data != value)
            {
                field.Data = value;
                field.Modified = true;

                if (row.Operation == RowOperation.None)
                {
                    row.Operation = RowOperation.Modify;
                }
            }

            return field.Modified;
        }

        /// <summary>
        /// Tests sequence table for PatchFiles and associated actions
        /// </summary>
        /// <param name="sequenceTable">The table to test.</param>
        /// <param name="hasPatchFilesAction">Set to true if PatchFiles action is found. Left unchanged otherwise.</param>
        /// <param name="installFilesSequence">Set to sequence value of InstallFiles action if found. Left unchanged otherwise.</param>
        /// <param name="duplicateFilesSequence">Set to sequence value of DuplicateFiles action if found. Left unchanged otherwise.</param>
        private static void TestSequenceTableForPatchFilesAction(Table sequenceTable, ref bool hasPatchFilesAction, ref int installFilesSequence, ref int duplicateFilesSequence)
        {
            if (null != sequenceTable)
            {
                foreach (var row in sequenceTable.Rows)
                {
                    var actionName = row.FieldAsString(0);
                    switch (actionName)
                    {
                        case "PatchFiles":
                            hasPatchFilesAction = true;
                            break;

                        case "InstallFiles":
                            installFilesSequence = row.FieldAsInteger(2);
                            break;

                        case "DuplicateFiles":
                            duplicateFilesSequence = row.FieldAsInteger(2);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Signal a warning if a non-keypath file was changed in a patch without also changing the keypath file of the component.
        /// </summary>
        /// <param name="transform">The output to validate.</param>
        private void ValidateFileRowChanges(WindowsInstallerData transform)
        {
            var componentTable = transform.Tables["Component"];
            var fileTable = transform.Tables["File"];

            // There's no sense validating keypaths if the transform has no component or file table
            if (componentTable == null || fileTable == null)
            {
                return;
            }

            // Index the Component table for non-directory & non-registry key paths.
            var componentKeyPath = new Dictionary<string, string>();
            foreach (var row in componentTable.Rows.Cast<ComponentRow>().Where(r => !r.IsRegistryKeyPath))
            {
                var keyPath = row.KeyPath;

                if (!String.IsNullOrEmpty(keyPath))
                {
                    componentKeyPath.Add(row.Component, keyPath);
                }
            }

            var componentWithChangedKeyPath = new Dictionary<string, string>();
            var componentWithNonKeyPathChanged = new Dictionary<string, string>();

            // Verify changes in the file table, now that file diffing has occurred
            foreach (var row in fileTable.Rows.Cast<FileRow>().Where(r => r.Operation == RowOperation.Modify))
            {
                var fileId = row.File;
                var componentId = row.Component;

                // If this file is the keypath of a component
                if (componentKeyPath.ContainsValue(fileId))
                {
                    if (!componentWithChangedKeyPath.ContainsKey(componentId))
                    {
                        componentWithChangedKeyPath.Add(componentId, fileId);
                    }
                }
                else
                {
                    if (!componentWithNonKeyPathChanged.ContainsKey(componentId))
                    {
                        componentWithNonKeyPathChanged.Add(componentId, fileId);
                    }
                }
            }

            foreach (var componentFile in componentWithNonKeyPathChanged)
            {
                // Make sure all changes to non keypath files also had a change in the keypath.
                if (!componentWithChangedKeyPath.ContainsKey(componentFile.Key) && componentKeyPath.TryGetValue(componentFile.Key, out var keyPath))
                {
                    this.Messaging.Write(WindowsInstallerBackendWarnings.UpdateOfNonKeyPathFile(componentFile.Value, componentFile.Key, keyPath));
                }
            }
        }
    }
}
