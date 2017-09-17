// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// AssignMediaCommand assigns files to cabs based on Media or MediaTemplate rows.
    /// </summary>
    public class AssignMediaCommand : ICommand
    {
        public AssignMediaCommand()
        {
            this.CabinetNameTemplate = "Cab{0}.cab";
        }

        public Output Output { private get; set; }

        public bool FilesCompressed { private get; set; }

        public string CabinetNameTemplate { private get; set; }

        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        /// <summary>
        /// Gets cabinets with their file rows.
        /// </summary>
        public Dictionary<MediaRow, IEnumerable<FileFacade>> FileFacadesByCabinetMedia { get; private set; }

        /// <summary>
        /// Get media rows.
        /// </summary>
        public RowDictionary<MediaRow> MediaRows { get; private set; }

        /// <summary>
        /// Get uncompressed file rows. This will contain file rows of File elements that are marked with compression=no.
        /// This contains all the files when Package element is marked with compression=no
        /// </summary>
        public IEnumerable<FileFacade> UncompressedFileFacades { get; private set; }

        public void Execute()
        {
            Dictionary<MediaRow, List<FileFacade>> filesByCabinetMedia = new Dictionary<MediaRow, List<FileFacade>>();

            RowDictionary<MediaRow> mediaRows = new RowDictionary<MediaRow>();

            List<FileFacade> uncompressedFiles = new List<FileFacade>();

            MediaRow mergeModuleMediaRow = null;
            Table mediaTable = this.Output.Tables["Media"];
            Table mediaTemplateTable = this.Output.Tables["WixMediaTemplate"];

            // If both tables are authored, it is an error.
            if ((mediaTemplateTable != null && mediaTemplateTable.Rows.Count > 0) && (mediaTable != null && mediaTable.Rows.Count > 1))
            {
                throw new WixException(WixErrors.MediaTableCollision(null));
            }

            // When building merge module, all the files go to "#MergeModule.CABinet".
            if (OutputType.Module == this.Output.Type)
            {
                Table mergeModuleMediaTable = new Table(null, this.TableDefinitions["Media"]);
                mergeModuleMediaRow = (MediaRow)mergeModuleMediaTable.CreateRow(null);
                mergeModuleMediaRow.Cabinet = "#MergeModule.CABinet";

                filesByCabinetMedia.Add(mergeModuleMediaRow, new List<FileFacade>());
            }

            if (OutputType.Module == this.Output.Type || null == mediaTemplateTable)
            {
                this.ManuallyAssignFiles(mediaTable, mergeModuleMediaRow, this.FileFacades, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }
            else
            {
                this.AutoAssignFiles(mediaTable, this.FileFacades, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }

            this.FileFacadesByCabinetMedia = new Dictionary<MediaRow, IEnumerable<FileFacade>>();

            foreach (var mediaRowWithFiles in filesByCabinetMedia)
            {
                this.FileFacadesByCabinetMedia.Add(mediaRowWithFiles.Key, mediaRowWithFiles.Value);
            }

            this.MediaRows = mediaRows;

            this.UncompressedFileFacades = uncompressedFiles;
        }

        /// <summary>
        /// Assign files to cabinets based on MediaTemplate authoring.
        /// </summary>
        /// <param name="fileFacades">FileRowCollection</param>
        private void AutoAssignFiles(Table mediaTable, IEnumerable<FileFacade> fileFacades, Dictionary<MediaRow, List<FileFacade>> filesByCabinetMedia, RowDictionary<MediaRow> mediaRows, List<FileFacade> uncompressedFiles)
        {
            const int MaxCabIndex = 999;

            ulong currentPreCabSize = 0;
            ulong maxPreCabSizeInBytes;
            int maxPreCabSizeInMB = 0;
            int currentCabIndex = 0;

            MediaRow currentMediaRow = null;

            Table mediaTemplateTable = this.Output.Tables["WixMediaTemplate"];

            // Auto assign files to cabinets based on maximum uncompressed media size
            mediaTable.Rows.Clear();
            WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)mediaTemplateTable.Rows[0];

            if (!String.IsNullOrEmpty(mediaTemplateRow.CabinetTemplate))
            {
                this.CabinetNameTemplate = mediaTemplateRow.CabinetTemplate;
            }

            string mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");

            try
            {
                // Override authored mums value if environment variable is authored.
                if (!String.IsNullOrEmpty(mumsString))
                {
                    maxPreCabSizeInMB = Int32.Parse(mumsString);
                }
                else
                {
                    maxPreCabSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize;
                }

                maxPreCabSizeInBytes = (ulong)maxPreCabSizeInMB * 1024 * 1024;
            }
            catch (FormatException)
            {
                throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
            }
            catch (OverflowException)
            {
                throw new WixException(WixErrors.MaximumUncompressedMediaSizeTooLarge(null, maxPreCabSizeInMB));
            }

            foreach (FileFacade facade in this.FileFacades)
            {
                // When building a product, if the current file is not to be compressed or if
                // the package set not to be compressed, don't cab it.
                if (OutputType.Product == this.Output.Type &&
                    (YesNoType.No == facade.File.Compressed ||
                    (YesNoType.NotSet == facade.File.Compressed && !this.FilesCompressed)))
                {
                    uncompressedFiles.Add(facade);
                    continue;
                }

                if (currentCabIndex == MaxCabIndex)
                {
                    // Associate current file with last cab (irrespective of the size) and cab index is not incremented anymore.
                    List<FileFacade> cabinetFiles = filesByCabinetMedia[currentMediaRow];
                    facade.WixFile.DiskId = currentCabIndex;
                    cabinetFiles.Add(facade);
                    continue;
                }

                // Update current cab size.
                currentPreCabSize += (ulong)facade.File.FileSize;

                if (currentPreCabSize > maxPreCabSizeInBytes)
                {
                    // Overflow due to current file
                    currentMediaRow = this.AddMediaRow(mediaTemplateRow, mediaTable, ++currentCabIndex);
                    mediaRows.Add(currentMediaRow);
                    filesByCabinetMedia.Add(currentMediaRow, new List<FileFacade>());

                    List<FileFacade> cabinetFileRows = filesByCabinetMedia[currentMediaRow];
                    facade.WixFile.DiskId = currentCabIndex;
                    cabinetFileRows.Add(facade);
                    // Now files larger than MaxUncompressedMediaSize will be the only file in its cabinet so as to respect MaxUncompressedMediaSize
                    currentPreCabSize = (ulong)facade.File.FileSize;
                }
                else
                {
                    // File fits in the current cab.
                    if (currentMediaRow == null)
                    {
                        // Create new cab and MediaRow
                        currentMediaRow = this.AddMediaRow(mediaTemplateRow, mediaTable, ++currentCabIndex);
                        mediaRows.Add(currentMediaRow);
                        filesByCabinetMedia.Add(currentMediaRow, new List<FileFacade>());
                    }

                    // Associate current file with current cab.
                    List<FileFacade> cabinetFiles = filesByCabinetMedia[currentMediaRow];
                    facade.WixFile.DiskId = currentCabIndex;
                    cabinetFiles.Add(facade);
                }
            }

            // If there are uncompressed files and no MediaRow, create a default one.
            if (uncompressedFiles.Count > 0 && mediaTable.Rows.Count == 0)
            {
                MediaRow defaultMediaRow = (MediaRow)mediaTable.CreateRow(null);
                defaultMediaRow.DiskId = 1;
                mediaRows.Add(defaultMediaRow);
            }
        }

        /// <summary>
        /// Assign files to cabinets based on Media authoring.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="mergeModuleMediaRow"></param>
        /// <param name="fileFacades"></param>
        private void ManuallyAssignFiles(Table mediaTable, MediaRow mergeModuleMediaRow, IEnumerable<FileFacade> fileFacades, Dictionary<MediaRow, List<FileFacade>> filesByCabinetMedia, RowDictionary<MediaRow> mediaRows, List<FileFacade> uncompressedFiles)
        {
            if (OutputType.Module != this.Output.Type)
            {
                if (null != mediaTable)
                {
                    Dictionary<string, MediaRow> cabinetMediaRows = new Dictionary<string, MediaRow>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (MediaRow mediaRow in mediaTable.Rows)
                    {
                        // If the Media row has a cabinet, make sure it is unique across all Media rows.
                        if (!String.IsNullOrEmpty(mediaRow.Cabinet))
                        {
                            MediaRow existingRow;
                            if (cabinetMediaRows.TryGetValue(mediaRow.Cabinet, out existingRow))
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateCabinetName(mediaRow.SourceLineNumbers, mediaRow.Cabinet));
                                Messaging.Instance.OnMessage(WixErrors.DuplicateCabinetName2(existingRow.SourceLineNumbers, existingRow.Cabinet));
                            }
                            else
                            {
                                cabinetMediaRows.Add(mediaRow.Cabinet, mediaRow);
                            }
                        }

                        mediaRows.Add(mediaRow);
                    }
                }

                foreach (MediaRow mediaRow in mediaRows.Values)
                {
                    if (null != mediaRow.Cabinet)
                    {
                        filesByCabinetMedia.Add(mediaRow, new List<FileFacade>());
                    }
                }
            }

            foreach (FileFacade facade in fileFacades)
            {
                if (OutputType.Module == this.Output.Type)
                {
                    filesByCabinetMedia[mergeModuleMediaRow].Add(facade);
                }
                else
                {
                    MediaRow mediaRow;
                    if (!mediaRows.TryGetValue(facade.WixFile.DiskId.ToString(CultureInfo.InvariantCulture), out mediaRow))
                    {
                        Messaging.Instance.OnMessage(WixErrors.MissingMedia(facade.File.SourceLineNumbers, facade.WixFile.DiskId));
                        continue;
                    }

                    // When building a product, if the current file is not to be compressed or if
                    // the package set not to be compressed, don't cab it.
                    if (OutputType.Product == this.Output.Type &&
                        (YesNoType.No == facade.File.Compressed ||
                        (YesNoType.NotSet == facade.File.Compressed && !this.FilesCompressed)))
                    {
                        uncompressedFiles.Add(facade);
                    }
                    else // file is marked compressed.
                    {
                        List<FileFacade> cabinetFiles;
                        if (filesByCabinetMedia.TryGetValue(mediaRow, out cabinetFiles))
                        {
                            cabinetFiles.Add(facade);
                        }
                        else
                        {
                            Messaging.Instance.OnMessage(WixErrors.ExpectedMediaCabinet(facade.File.SourceLineNumbers, facade.File.File, facade.WixFile.DiskId));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a row to the media table with cab name template filled in.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="cabIndex"></param>
        /// <returns></returns>
        private MediaRow AddMediaRow(WixMediaTemplateRow mediaTemplateRow, Table mediaTable, int cabIndex)
        {
            MediaRow currentMediaRow = (MediaRow)mediaTable.CreateRow(mediaTemplateRow.SourceLineNumbers);
            currentMediaRow.DiskId = cabIndex;
            currentMediaRow.Cabinet = String.Format(CultureInfo.InvariantCulture, this.CabinetNameTemplate, cabIndex);

            Table wixMediaTable = this.Output.EnsureTable(this.TableDefinitions["WixMedia"]);
            WixMediaRow row = (WixMediaRow)wixMediaTable.CreateRow(mediaTemplateRow.SourceLineNumbers);
            row.DiskId = cabIndex;
            row.CompressionLevel = mediaTemplateRow.CompressionLevel;

            return currentMediaRow;
        }
    }
}
