// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// AssignMediaCommand assigns files to cabs based on Media or MediaTemplate rows.
    /// </summary>
    internal class AssignMediaCommand
    {
        private const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB

        public AssignMediaCommand(IntermediateSection section, IMessaging messaging)
        {
            this.CabinetNameTemplate = "Cab{0}.cab";
            this.Section = section;
            this.Messaging = messaging;
        }

        private IntermediateSection Section { get; }

        private IMessaging Messaging { get; }

        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public bool FilesCompressed { private get; set; }

        public string CabinetNameTemplate { private get; set; }

        /// <summary>
        /// Gets cabinets with their file rows.
        /// </summary>
        public Dictionary<MediaTuple, IEnumerable<FileFacade>> FileFacadesByCabinetMedia { get; private set; }

        /// <summary>
        /// Get media rows.
        /// </summary>
        public Dictionary<int, MediaTuple> MediaRows { get; private set; }

        /// <summary>
        /// Get uncompressed file rows. This will contain file rows of File elements that are marked with compression=no.
        /// This contains all the files when Package element is marked with compression=no
        /// </summary>
        public IEnumerable<FileFacade> UncompressedFileFacades { get; private set; }

        public void Execute()
        {
            var filesByCabinetMedia = new Dictionary<MediaTuple, List<FileFacade>>();

            var mediaRows = new Dictionary<int, MediaTuple>();

            List<FileFacade> uncompressedFiles = new List<FileFacade>();

            var mediaTable = this.Section.Tuples.OfType<MediaTuple>().ToList();
            var mediaTemplateTable = this.Section.Tuples.OfType<WixMediaTemplateTuple>().ToList();

            // If both tables are authored, it is an error.
            if (mediaTemplateTable.Count > 0 && mediaTable.Count > 1)
            {
                throw new WixException(ErrorMessages.MediaTableCollision(null));
            }

            // When building merge module, all the files go to "#MergeModule.CABinet".
            if (SectionType.Module == this.Section.Type)
            {
                var mergeModuleMediaRow = new MediaTuple();
                mergeModuleMediaRow.Cabinet = "#MergeModule.CABinet";

                this.Section.Tuples.Add(mergeModuleMediaRow);

                filesByCabinetMedia.Add(mergeModuleMediaRow, new List<FileFacade>(this.FileFacades));
            }
            else if (mediaTemplateTable.Count == 0)
            {
                this.ManuallyAssignFiles(mediaTable, this.FileFacades, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }
            else
            {
                this.AutoAssignFiles(mediaTable, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }

            this.FileFacadesByCabinetMedia = new Dictionary<MediaTuple, IEnumerable<FileFacade>>();

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
        private void AutoAssignFiles(List<MediaTuple> mediaTable, Dictionary<MediaTuple, List<FileFacade>> filesByCabinetMedia, Dictionary<int, MediaTuple> mediaRows, List<FileFacade> uncompressedFiles)
        {
            const int MaxCabIndex = 999;

            ulong currentPreCabSize = 0;
            ulong maxPreCabSizeInBytes;
            int maxPreCabSizeInMB = 0;
            int currentCabIndex = 0;

            MediaTuple currentMediaRow = null;

            var mediaTemplateTable = this.Section.Tuples.OfType<WixMediaTemplateTuple>();

            // Remove all previous media tuples since they will be replaced with
            // media template.
            foreach (var mediaTuple in mediaTable)
            {
                this.Section.Tuples.Remove(mediaTuple);
            }

            // Auto assign files to cabinets based on maximum uncompressed media size
            var mediaTemplateRow = mediaTemplateTable.Single();

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
                    maxPreCabSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize ?? DefaultMaximumUncompressedMediaSize;
                }

                maxPreCabSizeInBytes = (ulong)maxPreCabSizeInMB * 1024 * 1024;
            }
            catch (FormatException)
            {
                throw new WixException(ErrorMessages.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
            }
            catch (OverflowException)
            {
                throw new WixException(ErrorMessages.MaximumUncompressedMediaSizeTooLarge(null, maxPreCabSizeInMB));
            }

            foreach (var facade in this.FileFacades)
            {
                // When building a product, if the current file is not to be compressed or if
                // the package set not to be compressed, don't cab it.
                if (SectionType.Product == this.Section.Type && (facade.Uncompressed || !this.FilesCompressed))
                {
                    uncompressedFiles.Add(facade);
                    continue;
                }

                if (currentCabIndex == MaxCabIndex)
                {
                    // Associate current file with last cab (irrespective of the size) and cab index is not incremented anymore.
                    var cabinetFiles = filesByCabinetMedia[currentMediaRow];
                    facade.File.DiskId = currentCabIndex;
                    cabinetFiles.Add(facade);
                    continue;
                }

                // Update current cab size.
                currentPreCabSize += (ulong)facade.File.FileSize;

                if (currentPreCabSize > maxPreCabSizeInBytes)
                {
                    // Overflow due to current file
                    currentMediaRow = this.AddMediaRow(mediaTemplateRow, ++currentCabIndex);
                    mediaRows.Add(currentMediaRow.DiskId, currentMediaRow);
                    filesByCabinetMedia.Add(currentMediaRow, new List<FileFacade>());

                    var cabinetFileRows = filesByCabinetMedia[currentMediaRow];
                    facade.File.DiskId = currentCabIndex;
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
                        currentMediaRow = this.AddMediaRow(mediaTemplateRow, ++currentCabIndex);
                        mediaRows.Add(currentMediaRow.DiskId, currentMediaRow);
                        filesByCabinetMedia.Add(currentMediaRow, new List<FileFacade>());
                    }

                    // Associate current file with current cab.
                    var cabinetFiles = filesByCabinetMedia[currentMediaRow];
                    facade.File.DiskId = currentCabIndex;
                    cabinetFiles.Add(facade);
                }
            }

            // If there are uncompressed files and no MediaRow, create a default one.
            if (uncompressedFiles.Count > 0 && !this.Section.Tuples.OfType<MediaTuple>().Any())
            {
                var defaultMediaRow = new MediaTuple(null, new Identifier(AccessModifier.Private, 1))
                {
                    DiskId = 1
                };

                mediaRows.Add(1, defaultMediaRow);
                this.Section.Tuples.Add(defaultMediaRow);
            }
        }

        /// <summary>
        /// Assign files to cabinets based on Media authoring.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="fileFacades"></param>
        private void ManuallyAssignFiles(List<MediaTuple> mediaTable, IEnumerable<FileFacade> fileFacades, Dictionary<MediaTuple, List<FileFacade>> filesByCabinetMedia, Dictionary<int, MediaTuple> mediaRows, List<FileFacade> uncompressedFiles)
        {
            if (mediaTable.Any())
            {
                var cabinetMediaRows = new Dictionary<string, MediaTuple>(StringComparer.OrdinalIgnoreCase);
                foreach (var mediaRow in mediaTable)
                {
                    // If the Media row has a cabinet, make sure it is unique across all Media rows.
                    if (!String.IsNullOrEmpty(mediaRow.Cabinet))
                    {
                        if (cabinetMediaRows.TryGetValue(mediaRow.Cabinet, out var existingRow))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateCabinetName(mediaRow.SourceLineNumbers, mediaRow.Cabinet));
                            this.Messaging.Write(ErrorMessages.DuplicateCabinetName2(existingRow.SourceLineNumbers, existingRow.Cabinet));
                        }
                        else
                        {
                            cabinetMediaRows.Add(mediaRow.Cabinet, mediaRow);
                        }
                    }

                    mediaRows.Add(mediaRow.DiskId, mediaRow);
                }
            }

            foreach (var mediaRow in mediaRows.Values)
            {
                if (null != mediaRow.Cabinet)
                {
                    filesByCabinetMedia.Add(mediaRow, new List<FileFacade>());
                }
            }

            foreach (FileFacade facade in fileFacades)
            {
                if (!mediaRows.TryGetValue(facade.DiskId, out var mediaRow))
                {
                    this.Messaging.Write(ErrorMessages.MissingMedia(facade.File.SourceLineNumbers, facade.DiskId));
                    continue;
                }

                // When building a product, if the current file is to be uncompressed or if
                // the package set not to be compressed, don't cab it.
                var compressed = (facade.File.Attributes & FileTupleAttributes.Compressed) == FileTupleAttributes.Compressed;
                var uncompressed = (facade.File.Attributes & FileTupleAttributes.Uncompressed) == FileTupleAttributes.Uncompressed;
                if (SectionType.Product == this.Section.Type && (uncompressed || (!compressed && !this.FilesCompressed)))
                {
                    uncompressedFiles.Add(facade);
                }
                else // file is marked compressed.
                {
                    if (filesByCabinetMedia.TryGetValue(mediaRow, out var cabinetFiles))
                    {
                        cabinetFiles.Add(facade);
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedMediaCabinet(facade.File.SourceLineNumbers, facade.File.Id.Id, facade.DiskId));
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
        private MediaTuple AddMediaRow(WixMediaTemplateTuple mediaTemplateTuple, int cabIndex)
        {
            var currentMediaTuple = new MediaTuple(mediaTemplateTuple.SourceLineNumbers, new Identifier(AccessModifier.Private, cabIndex));
            currentMediaTuple.DiskId = cabIndex;
            currentMediaTuple.Cabinet = String.Format(CultureInfo.InvariantCulture, this.CabinetNameTemplate, cabIndex);
            currentMediaTuple.CompressionLevel = mediaTemplateTuple.CompressionLevel;

            this.Section.Tuples.Add(currentMediaTuple);

            return currentMediaTuple;
        }
    }
}
