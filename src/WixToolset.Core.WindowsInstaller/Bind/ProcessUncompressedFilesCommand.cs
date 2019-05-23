// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Defines the file transfers necessary to layout the uncompressed files.
    /// </summary>
    internal class ProcessUncompressedFilesCommand
    {
        public ProcessUncompressedFilesCommand(IntermediateSection section, IBackendHelper backendHelper)
        {
            this.Section = section;
            this.BackendHelper = backendHelper;
        }

        private IntermediateSection Section { get; }

        public IBackendHelper BackendHelper { get; }

        public string DatabasePath { private get; set; }

        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public string LayoutDirectory { private get; set; }

        public bool Compressed { private get; set; }

        public bool LongNamesInImage { private get; set; }

        public Func<MediaTuple, string, string, string> ResolveMedia { private get; set; }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; private set; }

        public void Execute()
        {
            var fileTransfers = new List<IFileTransfer>();

            var trackedFiles = new List<ITrackedFile>();

            var directories = new Dictionary<string, ResolvedDirectory>();

            var mediaRows = this.Section.Tuples.OfType<MediaTuple>().ToDictionary(t => t.DiskId);

            using (Database db = new Database(this.DatabasePath, OpenDatabase.ReadOnly))
            {
                using (View directoryView = db.OpenExecuteView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                {
                    while (true)
                    {
                        using (Record directoryRecord = directoryView.Fetch())
                        {
                            if (null == directoryRecord)
                            {
                                break;
                            }

                            string sourceName = Common.GetName(directoryRecord.GetString(3), true, this.LongNamesInImage);

                            directories.Add(directoryRecord.GetString(1), new ResolvedDirectory(directoryRecord.GetString(2), sourceName));
                        }
                    }
                }

                using (View fileView = db.OpenView("SELECT `Directory_`, `FileName` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_` AND `File`.`File`=?"))
                {
                    using (Record fileQueryRecord = new Record(1))
                    {
                        // for each file in the array of uncompressed files
                        foreach (FileFacade facade in this.FileFacades)
                        {
                            var mediaTuple = mediaRows[facade.DiskId];
                            string relativeFileLayoutPath = null;
                            string mediaLayoutFolder = mediaTuple.Layout;

                            var mediaLayoutDirectory = this.ResolveMedia(mediaTuple, mediaLayoutFolder, this.LayoutDirectory);

                            // setup up the query record and find the appropriate file in the
                            // previously executed file view
                            fileQueryRecord[1] = facade.File.Id.Id;
                            fileView.Execute(fileQueryRecord);

                            using (Record fileRecord = fileView.Fetch())
                            {
                                if (null == fileRecord)
                                {
                                    throw new WixException(ErrorMessages.FileIdentifierNotFound(facade.File.SourceLineNumbers, facade.File.Id.Id));
                                }

                                relativeFileLayoutPath = PathResolver.GetFileSourcePath(directories, fileRecord[1], fileRecord[2], this.Compressed, this.LongNamesInImage);
                            }

                            // finally put together the base media layout path and the relative file layout path
                            var fileLayoutPath = Path.Combine(mediaLayoutDirectory, relativeFileLayoutPath);

                            var transfer = this.BackendHelper.CreateFileTransfer(facade.File.Source.Path, fileLayoutPath, false, facade.File.SourceLineNumbers);
                            fileTransfers.Add(transfer);

                            // Track the location where the cabinet will be placed. If the transfer is
                            // redundant then then the file should not be cleaned. This is important
                            // because if the source and destination of the transfer is the same, we
                            // don't want to clean the file because we'd be deleting the original
                            // (and that would be bad).
                            var tracked = this.BackendHelper.TrackFile(transfer.Destination, TrackedFileType.Final, facade.File.SourceLineNumbers);
                            tracked.Clean = !transfer.Redundant;

                            trackedFiles.Add(tracked);
                        }
                    }
                }
            }

            this.FileTransfers = fileTransfers;
            this.TrackedFiles = trackedFiles;
        }
    }
}
