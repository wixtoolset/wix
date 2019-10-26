// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Core.Native;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;

    internal class ExtractCabinetsCommand
    {
        public ExtractCabinetsCommand(WindowsInstallerData output, Database database, string inputFilePath, string exportBasePath, string intermediateFolder, bool treatOutputAsModule = false)
        {
            this.Output = output;
            this.Database = database;
            this.InputFilePath = inputFilePath;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;
            this.TreatOutputAsModule = treatOutputAsModule;
        }

        public string[] ExtractedFiles { get; private set; }

        private WindowsInstallerData Output { get; }

        private Database Database { get; }

        private string InputFilePath { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        public bool TreatOutputAsModule { get; }

        public void Execute()
        {
            var databaseBasePath = Path.GetDirectoryName(this.InputFilePath);
            var cabinetFiles = new List<string>();
            var embeddedCabinets = new SortedList();

            // index all of the cabinet files
            if (OutputType.Module == this.Output.Type || this.TreatOutputAsModule)
            {
                embeddedCabinets.Add(0, "MergeModule.CABinet");
            }
            else if (null != this.Output.Tables["Media"])
            {
                foreach (MediaRow mediaRow in this.Output.Tables["Media"].Rows)
                {
                    if (null != mediaRow.Cabinet)
                    {
                        if (OutputType.Product == this.Output.Type ||
                            (OutputType.Transform == this.Output.Type && RowOperation.Add == mediaRow.Operation))
                        {
                            if (mediaRow.Cabinet.StartsWith("#", StringComparison.Ordinal))
                            {
                                embeddedCabinets.Add(mediaRow.DiskId, mediaRow.Cabinet.Substring(1));
                            }
                            else
                            {
                                cabinetFiles.Add(Path.Combine(databaseBasePath, mediaRow.Cabinet));
                            }
                        }
                    }
                }
            }

            // extract the embedded cabinet files from the database
            if (0 < embeddedCabinets.Count)
            {
                using (var streamsView = this.Database.OpenView("SELECT `Data` FROM `_Streams` WHERE `Name` = ?"))
                {
                    foreach (int diskId in embeddedCabinets.Keys)
                    {
                        using (var record = new Record(1))
                        {
                            record.SetString(1, (string)embeddedCabinets[diskId]);
                            streamsView.Execute(record);
                        }

                        using (var record = streamsView.Fetch())
                        {
                            if (null != record)
                            {
                                // since the cabinets are stored in case-sensitive streams inside the msi, but the file system is not case-sensitive,
                                // embedded cabinets must be extracted to a canonical file name (like their diskid) to ensure extraction will always work
                                var cabinetFile = Path.Combine(this.IntermediateFolder, String.Concat("Media", Path.DirectorySeparatorChar, diskId.ToString(CultureInfo.InvariantCulture), ".cab"));

                                // ensure the parent directory exists
                                Directory.CreateDirectory(Path.GetDirectoryName(cabinetFile));

                                using (var fs = File.Create(cabinetFile))
                                {
                                    int bytesRead;
                                    var buffer = new byte[512];

                                    while (0 != (bytesRead = record.GetStream(1, buffer, buffer.Length)))
                                    {
                                        fs.Write(buffer, 0, bytesRead);
                                    }
                                }

                                cabinetFiles.Add(cabinetFile);
                            }
                            else
                            {
                                // TODO: warning about missing embedded cabinet
                            }
                        }
                    }
                }
            }

            // extract the cabinet files
            if (0 < cabinetFiles.Count)
            {
                // ensure the directory exists or extraction will fail
                Directory.CreateDirectory(this.ExportBasePath);

                foreach (var cabinetFile in cabinetFiles)
                {
                    try
                    {
                        var cabinet = new Cabinet(cabinetFile);
                        cabinet.Extract(this.ExportBasePath);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new WixException(ErrorMessages.FileNotFound(new SourceLineNumber(this.InputFilePath), cabinetFile));
                    }
                }

                this.ExtractedFiles = Directory.GetFiles(this.ExportBasePath);
            }
            else
            {
                this.ExtractedFiles = new string[0];
            }
        }
    }
}
