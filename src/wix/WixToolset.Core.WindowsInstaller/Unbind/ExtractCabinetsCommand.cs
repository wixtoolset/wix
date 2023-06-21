// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    internal class ExtractCabinetsCommand
    {
        public ExtractCabinetsCommand(IFileSystem fileSystem, WindowsInstallerData output, Database database, string inputFilePath, string exportBasePath, string intermediateFolder, bool treatOutputAsModule = false)
        {
            this.FileSystem = fileSystem;
            this.Output = output;
            this.Database = database;
            this.InputFilePath = inputFilePath;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;
            this.TreatOutputAsModule = treatOutputAsModule;
        }

        public Dictionary<string, MediaRow> ExtractedFileIdsWithMediaRow { get; private set; }

        private IFileSystem FileSystem { get; }

        private WindowsInstallerData Output { get; }

        private Database Database { get; }

        private string InputFilePath { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        public bool TreatOutputAsModule { get; }

        public void Execute()
        {
            var extractedFileIdsWithMediaRow = new Dictionary<string, MediaRow>();
            var databaseBasePath = Path.GetDirectoryName(this.InputFilePath);

            var cabinetPathsWithMediaRow = new Dictionary<string, MediaRow>();
            var embeddedCabinetNamesByDiskId = new SortedDictionary<int, string>();
            var embeddedCabinetRowsByDiskId = new SortedDictionary<int, MediaRow>();

            // index all of the cabinet files
            if (OutputType.Module == this.Output.Type || this.TreatOutputAsModule)
            {
                var mediaRow = new MediaRow(null, WindowsInstallerTableDefinitions.Media)
                {
                    DiskId = 1,
                    LastSequence = 1,
                    Cabinet = "MergeModule.CABinet",
                };

                embeddedCabinetRowsByDiskId.Add(1, mediaRow);
                embeddedCabinetNamesByDiskId.Add(1, "MergeModule.CABinet");
            }

            if (this.Output.Tables.TryGetTable("Media", out var mediaTable))
            {
                foreach (var mediaRow in mediaTable.Rows.Cast<MediaRow>().Where(r => !String.IsNullOrEmpty(r.Cabinet)))
                {
                    if (OutputType.Package == this.Output.Type ||
                        OutputType.Module == this.Output.Type ||
                        (OutputType.Transform == this.Output.Type && RowOperation.Add == mediaRow.Operation))
                    {
                        if (mediaRow.Cabinet.StartsWith("#", StringComparison.Ordinal))
                        {
                            embeddedCabinetNamesByDiskId.Add(mediaRow.DiskId, mediaRow.Cabinet.Substring(1));
                            embeddedCabinetRowsByDiskId.Add(mediaRow.DiskId, mediaRow);
                        }
                        else
                        {
                            cabinetPathsWithMediaRow.Add(Path.Combine(databaseBasePath, mediaRow.Cabinet), mediaRow);
                        }
                    }
                }
            }

            // Extract any embedded cabinet files from the database.
            if (0 < embeddedCabinetRowsByDiskId.Count)
            {
                using (var streamsView = this.Database.OpenView("SELECT `Data` FROM `_Streams` WHERE `Name` = ?"))
                {
                    foreach (var diskIdWithCabinetName in embeddedCabinetNamesByDiskId)
                    {
                        var diskId = diskIdWithCabinetName.Key;
                        var cabinetName = diskIdWithCabinetName.Value;

                        using (var record = new Record(1))
                        {
                            record.SetString(1, cabinetName);
                            streamsView.Execute(record);
                        }

                        using (var record = streamsView.Fetch())
                        {
                            if (null != record)
                            {
                                embeddedCabinetRowsByDiskId.TryGetValue(diskId, out var cabinetMediaRow);

                                // since the cabinets are stored in case-sensitive streams inside the msi, but the file system is not (typically) case-sensitive,
                                // embedded cabinets must be extracted to a canonical file name (like their diskid) to ensure extraction will always work
                                var cabinetPath = Path.Combine(this.IntermediateFolder, "Media", diskId.ToString(CultureInfo.InvariantCulture), ".cab");

                                // ensure the parent directory exists
                                Directory.CreateDirectory(Path.GetDirectoryName(cabinetPath));

                                using (var fs = this.FileSystem.OpenFile(cabinetMediaRow.SourceLineNumbers, cabinetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    int bytesRead;
                                    var buffer = new byte[4096];

                                    while (0 != (bytesRead = record.GetStream(1, buffer, buffer.Length)))
                                    {
                                        fs.Write(buffer, 0, bytesRead);
                                    }
                                }

                                cabinetPathsWithMediaRow.Add(cabinetPath, cabinetMediaRow);
                            }
                            else
                            {
                                // TODO: warning about missing embedded cabinet
                            }
                        }
                    }
                }
            }

            // Extract files from any available cabinets.
            if (0 < cabinetPathsWithMediaRow.Count)
            {
                Directory.CreateDirectory(this.ExportBasePath);

                foreach (var cabinetPathWithMediaRow in cabinetPathsWithMediaRow)
                {
                    var cabinetPath = cabinetPathWithMediaRow.Key;
                    var cabinetMediaRow = cabinetPathWithMediaRow.Value;

                    try
                    {
                        var cabinet = new Cabinet(cabinetPath);
                        var cabinetFilesExtracted = cabinet.Extract(this.ExportBasePath);

                        foreach (var extractedFile in cabinetFilesExtracted)
                        {
                            extractedFileIdsWithMediaRow.Add(extractedFile, cabinetMediaRow);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        throw new WixException(ErrorMessages.FileNotFound(new SourceLineNumber(this.InputFilePath), cabinetPath));
                    }
                }
            }

            this.ExtractedFileIdsWithMediaRow = extractedFileIdsWithMediaRow;
        }
    }
}
