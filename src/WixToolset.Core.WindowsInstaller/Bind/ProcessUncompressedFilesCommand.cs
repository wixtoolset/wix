// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Msi;
    using WixToolset.Core.Native;
    using WixToolset.Bind;
    using WixToolset.Core.Bind;
    using WixToolset.Data.Bind;
    using WixToolset.Data.Tuples;
    using System.Linq;

    /// <summary>
    /// Defines the file transfers necessary to layout the uncompressed files.
    /// </summary>
    internal class ProcessUncompressedFilesCommand
    {
        public ProcessUncompressedFilesCommand(IntermediateSection section)
        {
            this.Section = section;
        }

        private IntermediateSection Section { get; }

        public string DatabasePath { private get; set; }

        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public string LayoutDirectory { private get; set; }

        public bool Compressed { private get; set; }

        public bool LongNamesInImage { private get; set; }

        public Func<MediaTuple, string, string, string> ResolveMedia { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; private set; }

        public void Execute()
        {
            List<FileTransfer> fileTransfers = new List<FileTransfer>();

            var directories = new Dictionary<string, ResolvedDirectory>();

            var mediaRows = this.Section.Tuples.OfType<MediaTuple>().ToDictionary(t => t.DiskId);

            var wixMediaRows = this.Section.Tuples.OfType<WixMediaTuple>().ToDictionary(t => t.DiskId_);

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
                            var mediaTuple = mediaRows[facade.WixFile.DiskId];
                            string relativeFileLayoutPath = null;
                            string mediaLayoutFolder = null;

                            if (wixMediaRows.TryGetValue(facade.WixFile.DiskId, out var wixMediaRow))
                            {
                                mediaLayoutFolder = wixMediaRow.Layout;
                            }

                            var mediaLayoutDirectory = this.ResolveMedia(mediaTuple, mediaLayoutFolder, this.LayoutDirectory);

                            // setup up the query record and find the appropriate file in the
                            // previously executed file view
                            fileQueryRecord[1] = facade.File.File;
                            fileView.Execute(fileQueryRecord);

                            using (Record fileRecord = fileView.Fetch())
                            {
                                if (null == fileRecord)
                                {
                                    throw new WixException(WixErrors.FileIdentifierNotFound(facade.File.SourceLineNumbers, facade.File.File));
                                }

                                relativeFileLayoutPath = Binder.GetFileSourcePath(directories, fileRecord[1], fileRecord[2], this.Compressed, this.LongNamesInImage);
                            }

                            // finally put together the base media layout path and the relative file layout path
                            string fileLayoutPath = Path.Combine(mediaLayoutDirectory, relativeFileLayoutPath);
                            if (FileTransfer.TryCreate(facade.WixFile.Source, fileLayoutPath, false, "File", facade.File.SourceLineNumbers, out var transfer))
                            {
                                fileTransfers.Add(transfer);
                            }
                        }
                    }
                }
            }

            this.FileTransfers = fileTransfers;
        }
    }
}
