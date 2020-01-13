// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CopyTransformDataCommand
    {
        public bool CopyOutFileRows { private get; set; }

        public IEnumerable<IFileSystemExtension> Extensions { private get; set; }

        public IMessaging Messaging { private get; set; }

        public WindowsInstallerData Output { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public IEnumerable<FileFacade> FileFacades { get; private set; }

        public void Execute()
        {
            Debug.Assert(OutputType.Patch != this.Output.Type);

            List<FileFacade> allFileRows = this.CopyOutFileRows ? new List<FileFacade>() : null;

#if REVISIT_FOR_PATCHING // TODO: Fix this patching related code to work correctly with FileFacades.
            bool copyToPatch = (allFileRows != null);
            bool copyFromPatch = !copyToPatch;

            RowDictionary<MediaRow> patchMediaRows = new RowDictionary<MediaRow>();

            Dictionary<int, RowDictionary<WixFileRow>> patchMediaFileRows = new Dictionary<int, RowDictionary<WixFileRow>>();

            Table patchActualFileTable = this.Output.EnsureTable(this.TableDefinitions["File"]);
            Table patchFileTable = this.Output.EnsureTable(this.TableDefinitions["WixFile"]);

            if (copyFromPatch)
            {
                // index patch files by diskId+fileId
                foreach (WixFileRow patchFileRow in patchFileTable.Rows)
                {
                    int diskId = patchFileRow.DiskId;
                    RowDictionary<WixFileRow> mediaFileRows;
                    if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                    {
                        mediaFileRows = new RowDictionary<WixFileRow>();
                        patchMediaFileRows.Add(diskId, mediaFileRows);
                    }

                    mediaFileRows.Add(patchFileRow);
                }

                Table patchMediaTable = this.Output.EnsureTable(this.TableDefinitions["Media"]);
                patchMediaRows = new RowDictionary<MediaRow>(patchMediaTable);
            }

            // index paired transforms
            Dictionary<string, Output> pairedTransforms = new Dictionary<string, Output>();
            foreach (SubStorage substorage in this.Output.SubStorages)
            {
                if (substorage.Name.StartsWith("#"))
                {
                    pairedTransforms.Add(substorage.Name.Substring(1), substorage.Data);
                }
            }

            try
            {
                // copy File bind data into substorages
                foreach (SubStorage substorage in this.Output.SubStorages)
                {
                    if (substorage.Name.StartsWith("#"))
                    {
                        // no changes necessary for paired transforms
                        continue;
                    }

                    Output mainTransform = substorage.Data;
                    Table mainWixFileTable = mainTransform.Tables["WixFile"];
                    Table mainMsiFileHashTable = mainTransform.Tables["MsiFileHash"];

                    this.FileManagerCore.ActiveSubStorage = substorage;

                    RowDictionary<WixFileRow> mainWixFiles = new RowDictionary<WixFileRow>(mainWixFileTable);
                    RowDictionary<Row> mainMsiFileHashIndex = new RowDictionary<Row>();

                    Table mainFileTable = mainTransform.Tables["File"];
                    Output pairedTransform = (Output)pairedTransforms[substorage.Name];

                    // copy Media.LastSequence and index the MsiFileHash table if it exists.
                    if (copyFromPatch)
                    {
                        Table pairedMediaTable = pairedTransform.Tables["Media"];
                        foreach (MediaRow pairedMediaRow in pairedMediaTable.Rows)
                        {
                            MediaRow patchMediaRow = patchMediaRows.Get(pairedMediaRow.DiskId);
                            pairedMediaRow.Fields[1] = patchMediaRow.Fields[1];
                        }

                        if (null != mainMsiFileHashTable)
                        {
                            mainMsiFileHashIndex = new RowDictionary<Row>(mainMsiFileHashTable);
                        }

                        // Validate file row changes for keypath-related issues
                        this.ValidateFileRowChanges(mainTransform);
                    }

                    // Index File table of pairedTransform
                    Table pairedFileTable = pairedTransform.Tables["File"];
                    RowDictionary<FileRow> pairedFileRows = new RowDictionary<FileRow>(pairedFileTable);

                    if (null != mainFileTable)
                    {
                        if (copyFromPatch)
                        {
                            // Remove the MsiFileHash table because it will be updated later with the final file hash for each file
                            mainTransform.Tables.Remove("MsiFileHash");
                        }

                        foreach (FileRow mainFileRow in mainFileTable.Rows)
                        {
                            if (RowOperation.Delete == mainFileRow.Operation)
                            {
                                continue;
                            }
                            else if (RowOperation.None == mainFileRow.Operation && !copyToPatch)
                            {
                                continue;
                            }

                            WixFileRow mainWixFileRow = mainWixFiles.Get(mainFileRow.File);

                            if (copyToPatch) // when copying to the patch, we need compare the underlying files and include all file changes.
                            {
                                ObjectField objectField = (ObjectField)mainWixFileRow.Fields[6];
                                FileRow pairedFileRow = pairedFileRows.Get(mainFileRow.File);

                                // If the file is new, we always need to add it to the patch.
                                if (mainFileRow.Operation != RowOperation.Add)
                                {
                                    // If PreviousData doesn't exist, target and upgrade layout point to the same location. No need to compare.
                                    if (null == objectField.PreviousData)
                                    {
                                        if (mainFileRow.Operation == RowOperation.None)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // TODO: should this entire condition be placed in the binder file manager?
                                        if ((0 == (PatchAttributeType.Ignore & mainWixFileRow.PatchAttributes)) &&
                                            !this.CompareFiles(objectField.PreviousData.ToString(), objectField.Data.ToString()))
                                        {
                                            // If the file is different, we need to mark the mainFileRow and pairedFileRow as modified.
                                            mainFileRow.Operation = RowOperation.Modify;
                                            if (null != pairedFileRow)
                                            {
                                                // Always patch-added, but never non-compressed.
                                                pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                                pairedFileRow.Fields[6].Modified = true;
                                                pairedFileRow.Operation = RowOperation.Modify;
                                            }
                                        }
                                        else
                                        {
                                            // The File is same. We need mark all the attributes as unchanged.
                                            mainFileRow.Operation = RowOperation.None;
                                            foreach (Field field in mainFileRow.Fields)
                                            {
                                                field.Modified = false;
                                            }

                                            if (null != pairedFileRow)
                                            {
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Fields[6].Modified = false;
                                                pairedFileRow.Operation = RowOperation.None;
                                            }
                                            continue;
                                        }
                                    }
                                }
                                else if (null != pairedFileRow) // RowOperation.Add
                                {
                                    // Always patch-added, but never non-compressed.
                                    pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                    pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                    pairedFileRow.Fields[6].Modified = true;
                                    pairedFileRow.Operation = RowOperation.Add;
                                }
                            }

                            // index patch files by diskId+fileId
                            int diskId = mainWixFileRow.DiskId;

                            RowDictionary<WixFileRow> mediaFileRows;
                            if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                            {
                                mediaFileRows = new RowDictionary<WixFileRow>();
                                patchMediaFileRows.Add(diskId, mediaFileRows);
                            }

                            string fileId = mainFileRow.File;
                            WixFileRow patchFileRow = mediaFileRows.Get(fileId);
                            if (copyToPatch)
                            {
                                if (null == patchFileRow)
                                {
                                    FileRow patchActualFileRow = (FileRow)patchFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                                    patchActualFileRow.CopyFrom(mainFileRow);

                                    patchFileRow = (WixFileRow)patchFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                                    patchFileRow.CopyFrom(mainWixFileRow);

                                    mediaFileRows.Add(patchFileRow);

                                    allFileRows.Add(new FileFacade(patchActualFileRow, patchFileRow, null)); // TODO: should we be passing along delta information? Probably, right?
                                }
                                else
                                {
                                    // TODO: confirm the rest of data is identical?

                                    // make sure Source is same. Otherwise we are silently ignoring a file.
                                    if (0 != String.Compare(patchFileRow.Source, mainWixFileRow.Source, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.SameFileIdDifferentSource(mainFileRow.SourceLineNumbers, fileId, patchFileRow.Source, mainWixFileRow.Source));
                                    }

                                    // capture the previous file versions (and associated data) from this targeted instance of the baseline into the current filerow.
                                    patchFileRow.AppendPreviousDataFrom(mainWixFileRow);
                                }
                            }
                            else
                            {
                                // copy data from the patch back to the transform
                                if (null != patchFileRow)
                                {
                                    FileRow pairedFileRow = (FileRow)pairedFileRows.Get(fileId);
                                    for (int i = 0; i < patchFileRow.Fields.Length; i++)
                                    {
                                        string patchValue = patchFileRow[i] == null ? "" : patchFileRow[i].ToString();
                                        string mainValue = mainFileRow[i] == null ? "" : mainFileRow[i].ToString();

                                        if (1 == i)
                                        {
                                            // File.Component_ changes should not come from the shared file rows
                                            // that contain the file information as each individual transform might
                                            // have different changes (or no changes at all).
                                        }
                                        // File.Attributes should not changed for binary deltas
                                        else if (6 == i)
                                        {
                                            if (null != patchFileRow.Patch)
                                            {
                                                // File.Attribute should not change for binary deltas
                                                pairedFileRow.Attributes = mainFileRow.Attributes;
                                                mainFileRow.Fields[i].Modified = false;
                                            }
                                        }
                                        // File.Sequence is updated in pairedTransform, not mainTransform
                                        else if (7 == i)
                                        {
                                            // file sequence is updated in Patch table instead of File table for delta patches
                                            if (null != patchFileRow.Patch)
                                            {
                                                pairedFileRow.Fields[i].Modified = false;
                                            }
                                            else
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

                                    // copy MsiFileHash row for this File
                                    Row patchHashRow;
                                    if (!mainMsiFileHashIndex.TryGetValue(patchFileRow.File, out patchHashRow))
                                    {
                                        patchHashRow = patchFileRow.Hash;
                                    }

                                    if (null != patchHashRow)
                                    {
                                        Table mainHashTable = mainTransform.EnsureTable(this.TableDefinitions["MsiFileHash"]);
                                        Row mainHashRow = mainHashTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        for (int i = 0; i < patchHashRow.Fields.Length; i++)
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
                                    List<Row> patchAssemblyNameRows = patchFileRow.AssemblyNames;
                                    if (null != patchAssemblyNameRows)
                                    {
                                        Table mainAssemblyNameTable = mainTransform.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                                        foreach (Row patchAssemblyNameRow in patchAssemblyNameRows)
                                        {
                                            // Copy if there isn't an identical modified/added row already in the transform.
                                            bool foundMatchingModifiedRow = false;
                                            foreach (Row mainAssemblyNameRow in mainAssemblyNameTable.Rows)
                                            {
                                                if (RowOperation.None != mainAssemblyNameRow.Operation && mainAssemblyNameRow.GetPrimaryKey('/').Equals(patchAssemblyNameRow.GetPrimaryKey('/')))
                                                {
                                                    foundMatchingModifiedRow = true;
                                                    break;
                                                }
                                            }

                                            if (!foundMatchingModifiedRow)
                                            {
                                                Row mainAssemblyNameRow = mainAssemblyNameTable.CreateRow(mainFileRow.SourceLineNumbers);
                                                for (int i = 0; i < patchAssemblyNameRow.Fields.Length; i++)
                                                {
                                                    mainAssemblyNameRow[i] = patchAssemblyNameRow[i];
                                                }

                                                // assume value field has been modified
                                                mainAssemblyNameRow.Fields[2].Modified = true;
                                                mainAssemblyNameRow.Operation = mainFileRow.Operation;
                                            }
                                        }
                                    }

                                    // Add patch header for this file
                                    if (null != patchFileRow.Patch)
                                    {
                                        // Add the PatchFiles action automatically to the AdminExecuteSequence and InstallExecuteSequence tables.
                                        AddPatchFilesActionToSequenceTable(SequenceTable.AdminExecuteSequence, mainTransform, pairedTransform, mainFileRow);
                                        AddPatchFilesActionToSequenceTable(SequenceTable.InstallExecuteSequence, mainTransform, pairedTransform, mainFileRow);

                                        // Add to Patch table
                                        Table patchTable = pairedTransform.EnsureTable(this.TableDefinitions["Patch"]);
                                        if (0 == patchTable.Rows.Count)
                                        {
                                            patchTable.Operation = TableOperation.Add;
                                        }

                                        Row patchRow = patchTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        patchRow[0] = patchFileRow.File;
                                        patchRow[1] = patchFileRow.Sequence;

                                        FileInfo patchFile = new FileInfo(patchFileRow.Source);
                                        patchRow[2] = (int)patchFile.Length;
                                        patchRow[3] = 0 == (PatchAttributeType.AllowIgnoreOnError & patchFileRow.PatchAttributes) ? 0 : 1;

                                        string streamName = patchTable.Name + "." + patchRow[0] + "." + patchRow[1];
                                        if (MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                        {
                                            streamName = "_" + Guid.NewGuid().ToString("D").ToUpperInvariant().Replace('-', '_');
                                            Table patchHeadersTable = pairedTransform.EnsureTable(this.TableDefinitions["MsiPatchHeaders"]);
                                            if (0 == patchHeadersTable.Rows.Count)
                                            {
                                                patchHeadersTable.Operation = TableOperation.Add;
                                            }
                                            Row patchHeadersRow = patchHeadersTable.CreateRow(mainFileRow.SourceLineNumbers);
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
                                }
                                else
                                {
                                    // TODO: throw because all transform rows should have made it into the patch
                                }
                            }
                        }
                    }

                    if (copyFromPatch)
                    {
                        this.Output.Tables.Remove("Media");
                        this.Output.Tables.Remove("File");
                        this.Output.Tables.Remove("MsiFileHash");
                        this.Output.Tables.Remove("MsiAssemblyName");
                    }
                }
            }
            finally
            {
                this.FileManagerCore.ActiveSubStorage = null;
            }
#endif
            this.FileFacades = allFileRows;
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
                WindowsInstallerStandard.TryGetStandardAction(tableName, "PatchFiles", out var patchFilesActionTuple);

                var sequence = patchFilesActionTuple.Sequence;

                // Test for default sequence value's appropriateness
                if (installFilesSequence >= sequence || (0 != duplicateFilesSequence && duplicateFilesSequence <= sequence))
                {
                    if (0 != duplicateFilesSequence)
                    {
                        if (duplicateFilesSequence < installFilesSequence)
                        {
                            throw new WixException(ErrorMessages.InsertInvalidSequenceActionOrder(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionTuple.Action));
                        }
                        else
                        {
                            sequence = (duplicateFilesSequence + installFilesSequence) / 2;
                            if (installFilesSequence == sequence || duplicateFilesSequence == sequence)
                            {
                                throw new WixException(ErrorMessages.InsertSequenceNoSpace(mainFileRow.SourceLineNumbers, tableName, "InstallFiles", "DuplicateFiles", patchFilesActionTuple.Action));
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
                patchAction[0] = patchFilesActionTuple.Action;
                patchAction[1] = patchFilesActionTuple.Condition;
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
                    if (String.Equals("PatchFiles", actionName, StringComparison.Ordinal))
                    {
                        hasPatchFilesAction = true;
                    }
                    else if (String.Equals("InstallFiles", actionName, StringComparison.Ordinal))
                    {
                        installFilesSequence = row.FieldAsInteger(2);
                    }
                    else if (String.Equals("DuplicateFiles", actionName, StringComparison.Ordinal))
                    {
                        duplicateFilesSequence = row.FieldAsInteger(2);
                    }
                }
            }
        }

        /// <summary>
        /// Signal a warning if a non-keypath file was changed in a patch without also changing the keypath file of the component.
        /// </summary>
        /// <param name="output">The output to validate.</param>
        private void ValidateFileRowChanges(WindowsInstallerData transform)
        {
            Table componentTable = transform.Tables["Component"];
            Table fileTable = transform.Tables["File"];

            // There's no sense validating keypaths if the transform has no component or file table
            if (componentTable == null || fileTable == null)
            {
                return;
            }

            Dictionary<string, string> componentKeyPath = new Dictionary<string, string>(componentTable.Rows.Count);

            // Index the Component table for non-directory & non-registry key paths.
            foreach (Row row in componentTable.Rows)
            {
                if (null != row.Fields[5].Data &&
                    0 != ((int)row.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath))
                {
                    componentKeyPath.Add(row.Fields[0].Data.ToString(), row.Fields[5].Data.ToString());
                }
            }

            Dictionary<string, string> componentWithChangedKeyPath = new Dictionary<string, string>();
            Dictionary<string, string> componentWithNonKeyPathChanged = new Dictionary<string, string>();
            // Verify changes in the file table, now that file diffing has occurred
            foreach (FileRow row in fileTable.Rows)
            {
                string fileId = row.Fields[0].Data.ToString();
                string componentId = row.Fields[1].Data.ToString();

                if (RowOperation.Modify != row.Operation)
                {
                    continue;
                }

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

            foreach (KeyValuePair<string, string> componentFile in componentWithNonKeyPathChanged)
            {
                // Make sure all changes to non keypath files also had a change in the keypath.
                if (!componentWithChangedKeyPath.ContainsKey(componentFile.Key) && componentKeyPath.ContainsKey(componentFile.Key))
                {
                    this.Messaging.Write(WarningMessages.UpdateOfNonKeyPathFile((string)componentFile.Value, (string)componentFile.Key, (string)componentKeyPath[componentFile.Key]));
                }
            }
        }

        private bool CompareFiles(string targetFile, string updatedFile)
        {
            bool? compared = null;
            foreach (var extension in this.Extensions)
            {
                compared = extension.CompareFiles(targetFile, updatedFile);

                if (compared.HasValue)
                {
                    break;
                }
            }

            if (!compared.HasValue)
            {
                throw new InvalidOperationException(); // TODO: something needs to be said here that none of the binder file managers returned a result.
            }

            return compared.Value;
        }
    }
}
