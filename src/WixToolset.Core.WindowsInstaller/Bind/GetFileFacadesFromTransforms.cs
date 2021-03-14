// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class GetFileFacadesFromTransforms
    {
        public GetFileFacadesFromTransforms(IMessaging messaging, IWindowsInstallerBackendHelper backendHelper, FileSystemManager fileSystemManager, IEnumerable<SubStorage> subStorages)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystemManager = fileSystemManager;
            this.SubStorages = subStorages;
        }

        private IMessaging Messaging { get; }

        private IWindowsInstallerBackendHelper BackendHelper { get; }

        private FileSystemManager FileSystemManager { get; }

        private IEnumerable<SubStorage> SubStorages { get; }

        public List<IFileFacade> FileFacades { get; private set; }

        public void Execute()
        {
            var allFileRows = new List<IFileFacade>();

            var patchMediaFileRows = new Dictionary<int, RowDictionary<FileRow>>();

            //var patchActualFileTable = this.Output.EnsureTable(this.TableDefinitions["File"]);

            // Index paired transforms by name without their "#" prefix.
            var pairedTransforms = this.SubStorages.Where(s => s.Name.StartsWith("#")).ToDictionary(s => s.Name, s => s.Data);

            // Enumerate through main transforms.
            foreach (var substorage in this.SubStorages.Where(s => !s.Name.StartsWith("#")))
            {
                var mainTransform = substorage.Data;
                var mainFileTable = mainTransform.Tables["File"];

                if (null == mainFileTable)
                {
                    continue;
                }

                // Index File table of pairedTransform
                var pairedTransform = pairedTransforms["#" + substorage.Name];
                var pairedFileRows = new RowDictionary<FileRow>(pairedTransform.Tables["File"]);

                foreach (FileRow mainFileRow in mainFileTable.Rows.Where(f => f.Operation != RowOperation.Delete))
                {
                    var mainFileId = mainFileRow.File;

                    // We need compare the underlying files and include all file changes.
                    var objectField = (ObjectField)mainFileRow.Fields[9];
                    var pairedFileRow = pairedFileRows.Get(mainFileId);

                    // If the file is new, we always need to add it to the patch.
                    if (mainFileRow.Operation == RowOperation.Add)
                    {
                        if (null != pairedFileRow) // RowOperation.Add
                        {
                            // Always patch-added, but never non-compressed.
                            pairedFileRow.Attributes |= WindowsInstallerConstants.MsidbFileAttributesPatchAdded;
                            pairedFileRow.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                            pairedFileRow.Fields[6].Modified = true;
                            pairedFileRow.Operation = RowOperation.Add;
                        }
                    }
                    else
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
                            if (/*(0 == (PatchAttributeType.Ignore & mainWixFileRow.PatchAttributes)) &&*/
                                !this.FileSystemManager.CompareFiles(objectField.PreviousData.ToString(), objectField.Data.ToString()))
                            {
                                // If the file is different, we need to mark the mainFileRow and pairedFileRow as modified.
                                mainFileRow.Operation = RowOperation.Modify;
                                if (null != pairedFileRow)
                                {
                                    // Always patch-added, but never non-compressed.
                                    pairedFileRow.Attributes |= WindowsInstallerConstants.MsidbFileAttributesPatchAdded;
                                    pairedFileRow.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                                    pairedFileRow.Fields[6].Modified = true;
                                    pairedFileRow.Operation = RowOperation.Modify;
                                }
                            }
                            else
                            {
                                // The File is same. We need mark all the attributes as unchanged.
                                mainFileRow.Operation = RowOperation.None;
                                foreach (var field in mainFileRow.Fields)
                                {
                                    field.Modified = false;
                                }

                                if (null != pairedFileRow)
                                {
                                    pairedFileRow.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesPatchAdded;
                                    pairedFileRow.Fields[6].Modified = false;
                                    pairedFileRow.Operation = RowOperation.None;
                                }
                                continue;
                            }
                        }
                    }

                    // index patch files by diskId+fileId
                    var diskId = mainFileRow.DiskId;

                    if (!patchMediaFileRows.TryGetValue(diskId, out var mediaFileRows))
                    {
                        mediaFileRows = new RowDictionary<FileRow>();
                        patchMediaFileRows.Add(diskId, mediaFileRows);
                    }

                    var patchFileRow = mediaFileRows.Get(mainFileId);

                    if (null == patchFileRow)
                    {
                        //patchFileRow = (FileRow)patchFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                        patchFileRow = (FileRow)mainFileRow.TableDefinition.CreateRow(mainFileRow.SourceLineNumbers);
                        mainFileRow.CopyTo(patchFileRow);

                        mediaFileRows.Add(patchFileRow);

#if TODO_PATCHING_DELTA
                        // TODO: should we be passing along delta information to the file facade? Probably, right?
#endif
                        var fileFacade = this.BackendHelper.CreateFileFacade(patchFileRow);

                        allFileRows.Add(fileFacade);
                    }
                    else
                    {
                        // TODO: confirm the rest of data is identical?

                        // make sure Source is same. Otherwise we are silently ignoring a file.
                        if (0 != String.Compare(patchFileRow.Source, mainFileRow.Source, StringComparison.OrdinalIgnoreCase))
                        {
                            this.Messaging.Write(ErrorMessages.SameFileIdDifferentSource(mainFileRow.SourceLineNumbers, mainFileId, patchFileRow.Source, mainFileRow.Source));
                        }

#if TODO_PATCHING_DELTA
                        // capture the previous file versions (and associated data) from this targeted instance of the baseline into the current filerow.
                        patchFileRow.AppendPreviousDataFrom(mainFileRow);
#endif
                    }
                }
            }

            this.FileFacades = allFileRows;
        }
    }
}
