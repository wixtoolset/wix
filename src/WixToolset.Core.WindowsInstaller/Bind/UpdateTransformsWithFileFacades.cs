// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    internal class UpdateTransformsWithFileFacades
    {
        public UpdateTransformsWithFileFacades(IMessaging messaging, WindowsInstallerData output, IEnumerable<SubStorage> subStorages, TableDefinitionCollection tableDefinitions, IEnumerable<FileFacade> fileFacades)
        {
            this.Messaging = messaging;
            this.Output = output;
            this.SubStorages = subStorages;
            this.TableDefinitions = tableDefinitions;
            this.FileFacades = fileFacades;
        }

        private IMessaging Messaging { get; }

        private WindowsInstallerData Output { get; }

        private IEnumerable<SubStorage> SubStorages { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private IEnumerable<FileFacade> FileFacades { get; }

        public void Execute()
        {
            var fileFacadesByDiskId = new Dictionary<int, Dictionary<string, FileFacade>>();

            // Index patch file facades by diskId+fileId.
            foreach (var facade in this.FileFacades)
            {
                if (!fileFacadesByDiskId.TryGetValue(facade.DiskId, out var mediaFacades))
                {
                    mediaFacades = new Dictionary<string, FileFacade>();
                    fileFacadesByDiskId.Add(facade.DiskId, mediaFacades);
                }

                mediaFacades.Add(facade.Id, facade);
            }

            var patchMediaRows = new RowDictionary<MediaRow>(this.Output.Tables["Media"]);

            // Index paired transforms by name without the "#" prefix.
            var pairedTransforms = this.SubStorages.Where(s => s.Name.StartsWith("#")).ToDictionary(s => s.Name, s => s.Data);

            // Copy File bind data into substorages
            foreach (var substorage in this.SubStorages.Where(s => !s.Name.StartsWith("#")))
            {
                var mainTransform = substorage.Data;

                var mainMsiFileHashIndex = new RowDictionary<Row>(mainTransform.Tables["MsiFileHash"]);

                var pairedTransform = pairedTransforms["#" + substorage.Name];

                // Copy Media.LastSequence.
                var pairedMediaTable = pairedTransform.Tables["Media"];
                foreach (MediaRow pairedMediaRow in pairedMediaTable.Rows)
                {
                    var patchMediaRow = patchMediaRows.Get(pairedMediaRow.DiskId);
                    pairedMediaRow.LastSequence = patchMediaRow.LastSequence;
                }

                // Validate file row changes for keypath-related issues
                this.ValidateFileRowChanges(mainTransform);

                // Index File table of pairedTransform
                var pairedFileRows = new RowDictionary<FileRow>(pairedTransform.Tables["File"]);

                var mainFileTable = mainTransform.Tables["File"];
                if (null != mainFileTable)
                {
                    // Remove the MsiFileHash table because it will be updated later with the final file hash for each file
                    mainTransform.Tables.Remove("MsiFileHash");

                    foreach (FileRow mainFileRow in mainFileTable.Rows)
                    {
                        if (RowOperation.Delete == mainFileRow.Operation)
                        {
                            continue;
                        }
                        else if (RowOperation.None == mainFileRow.Operation)
                        {
                            continue;
                        }

                        // Index patch files by diskId+fileId
                        if (!fileFacadesByDiskId.TryGetValue(mainFileRow.DiskId, out var mediaFacades))
                        {
                            mediaFacades = new Dictionary<string, FileFacade>();
                            fileFacadesByDiskId.Add(mainFileRow.DiskId, mediaFacades);
                        }

                        // copy data from the patch back to the transform
                        if (mediaFacades.TryGetValue(mainFileRow.File, out var facade))
                        {
                            var patchFileRow = facade.GetFileRow();
                            var pairedFileRow = pairedFileRows.Get(mainFileRow.File);

                            for (var i = 0; i < patchFileRow.Fields.Length; i++)
                            {
                                var patchValue = patchFileRow.FieldAsString(i) ?? String.Empty;
                                var mainValue = mainFileRow.FieldAsString(i) ?? String.Empty;

                                if (1 == i)
                                {
                                    // File.Component_ changes should not come from the shared file rows
                                    // that contain the file information as each individual transform might
                                    // have different changes (or no changes at all).
                                }
                                else if (6 == i) // File.Attributes should not changed for binary deltas
                                {
#if TODO_PATCHING_DELTA
                                    if (null != patchFileRow.Patch)
                                    {
                                        // File.Attribute should not change for binary deltas
                                        pairedFileRow.Attributes = mainFileRow.Attributes;
                                        mainFileRow.Fields[i].Modified = false;
                                    }
#endif
                                }
                                else if (7 == i) // File.Sequence is updated in pairedTransform, not mainTransform
                                {
                                    // file sequence is updated in Patch table instead of File table for delta patches
#if TODO_PATCHING_DELTA
                                    if (null != patchFileRow.Patch)
                                    {
                                        pairedFileRow.Fields[i].Modified = false;
                                    }
                                    else
#endif
                                    {
                                        pairedFileRow[i] = patchFileRow[i];
                                        pairedFileRow.Fields[i].Modified = true;
                                    }
                                    mainFileRow.Fields[i].Modified = false;
                                }
                                else if (patchValue != mainValue)
                                {
                                    mainFileRow[i] = patchFileRow[i];
                                    mainFileRow.Fields[i].Modified = true;
                                    if (mainFileRow.Operation == RowOperation.None)
                                    {
                                        mainFileRow.Operation = RowOperation.Modify;
                                    }
                                }
                            }

                            // Copy MsiFileHash row for this File.
                            if (!mainMsiFileHashIndex.TryGetValue(patchFileRow.File, out var patchHashRow))
                            {
                                //patchHashRow = patchFileRow.Hash;
                                throw new NotImplementedException();
                            }

                            if (null != patchHashRow)
                            {
                                var mainHashTable = mainTransform.EnsureTable(this.TableDefinitions["MsiFileHash"]);
                                var mainHashRow = mainHashTable.CreateRow(mainFileRow.SourceLineNumbers);
                                for (var i = 0; i < patchHashRow.Fields.Length; i++)
                                {
                                    mainHashRow[i] = patchHashRow[i];
                                    if (i > 1)
                                    {
                                        // assume all hash fields have been modified
                                        mainHashRow.Fields[i].Modified = true;
                                    }
                                }

                                // assume the MsiFileHash operation follows the File one
                                mainHashRow.Operation = mainFileRow.Operation;
                            }

                            // copy MsiAssemblyName rows for this File
#if TODO_PATCHING
                            List<Row> patchAssemblyNameRows = patchFileRow.AssemblyNames;
                            if (null != patchAssemblyNameRows)
                            {
                                var mainAssemblyNameTable = mainTransform.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                                foreach (var patchAssemblyNameRow in patchAssemblyNameRows)
                                {
                                    // Copy if there isn't an identical modified/added row already in the transform.
                                    var foundMatchingModifiedRow = false;
                                    foreach (var mainAssemblyNameRow in mainAssemblyNameTable.Rows)
                                    {
                                        if (RowOperation.None != mainAssemblyNameRow.Operation && mainAssemblyNameRow.GetPrimaryKey('/').Equals(patchAssemblyNameRow.GetPrimaryKey('/')))
                                        {
                                            foundMatchingModifiedRow = true;
                                            break;
                                        }
                                    }

                                    if (!foundMatchingModifiedRow)
                                    {
                                        var mainAssemblyNameRow = mainAssemblyNameTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        for (var i = 0; i < patchAssemblyNameRow.Fields.Length; i++)
                                        {
                                            mainAssemblyNameRow[i] = patchAssemblyNameRow[i];
                                        }

                                        // assume value field has been modified
                                        mainAssemblyNameRow.Fields[2].Modified = true;
                                        mainAssemblyNameRow.Operation = mainFileRow.Operation;
                                    }
                                }
                            }
#endif

                            // Add patch header for this file
#if TODO_PATCHING_DELTA
                            if (null != patchFileRow.Patch)
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
                                patchRow[0] = patchFileRow.File;
                                patchRow[1] = patchFileRow.Sequence;

                                var patchFile = new FileInfo(patchFileRow.Source);
                                patchRow[2] = (int)patchFile.Length;
                                patchRow[3] = 0 == (PatchAttributeType.AllowIgnoreOnError & patchFileRow.PatchAttributes) ? 0 : 1;

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
                                    patchHeadersRow[1] = patchFileRow.Patch;
                                    patchRow[5] = streamName;
                                    patchHeadersRow.Operation = RowOperation.Add;
                                }
                                else
                                {
                                    patchRow[4] = patchFileRow.Patch;
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

                this.Output.Tables.Remove("Media");
                this.Output.Tables.Remove("File");
                this.Output.Tables.Remove("MsiFileHash");
                this.Output.Tables.Remove("MsiAssemblyName");
            }
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
                            throw new WixException(ErrorMessages.InsertInvalidSequenceActionOrder(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionSymbol.Action));
                        }
                        else
                        {
                            sequence = (duplicateFilesSequence + installFilesSequence) / 2;
                            if (installFilesSequence == sequence || duplicateFilesSequence == sequence)
                            {
                                throw new WixException(ErrorMessages.InsertSequenceNoSpace(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionSymbol.Action));
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

            var componentKeyPath = new Dictionary<string, string>(componentTable.Rows.Count);

            // Index the Component table for non-directory & non-registry key paths.
            foreach (var row in componentTable.Rows)
            {
                var keyPath = row.FieldAsString(5);
                if (keyPath != null && 0 != (row.FieldAsInteger(3) & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath))
                {
                    componentKeyPath.Add(row.FieldAsString(0), keyPath);
                }
            }

            var componentWithChangedKeyPath = new Dictionary<string, string>();
            var componentWithNonKeyPathChanged = new Dictionary<string, string>();
            // Verify changes in the file table, now that file diffing has occurred
            foreach (FileRow row in fileTable.Rows)
            {
                if (RowOperation.Modify != row.Operation)
                {
                    continue;
                }

                var fileId = row.FieldAsString(0);
                var componentId = row.FieldAsString(1);

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
                    this.Messaging.Write(WarningMessages.UpdateOfNonKeyPathFile(componentFile.Value, componentFile.Key, keyPath));
                }
            }
        }
    }
}
