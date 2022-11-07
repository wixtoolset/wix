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
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Creates cabinet files.
    /// </summary>
    internal class CreateCabinetsCommand
    {
        public const int DefaultMaximumUncompressedMediaSize = 200;             // Default value is 200 MB
        public const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024;  // 2048 MB (i.e. 2 GB)

        private readonly CabinetResolver cabinetResolver;
        private readonly List<IFileTransfer> fileTransfers;
        private readonly List<ITrackedFile> trackedFiles;

        public CreateCabinetsCommand(IServiceProvider serviceProvider, IMessaging messaging, IBackendHelper backendHelper, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions, IntermediateSection section, string cabCachePath, int cabbingThreadCount, string outputPath, string intermediateFolder, CompressionLevel? defaultCompressionLevel, bool compressed, string modularizationSuffix, Dictionary<MediaSymbol, IEnumerable<IFileFacade>> filesByCabinetMedia, WindowsInstallerData data, TableDefinitionCollection tableDefinitions, Func<MediaSymbol, string, string, string> resolveMedia)
        {
            this.Messaging = messaging;

            this.BackendHelper = backendHelper;

            this.Section = section;

            this.CabbingThreadCount = cabbingThreadCount;

            this.IntermediateFolder = intermediateFolder;
            this.LayoutDirectory = Path.GetDirectoryName(outputPath);

            this.DefaultCompressionLevel = defaultCompressionLevel;
            this.ModularizationSuffix = modularizationSuffix;
            this.FileFacadesByCabinet = filesByCabinetMedia;

            this.Data = data;
            this.TableDefinitions = tableDefinitions;

            this.ResolveMedia = resolveMedia;

            this.cabinetResolver = new CabinetResolver(serviceProvider, cabCachePath, backendExtensions);
            this.fileTransfers = new List<IFileTransfer>();
            this.trackedFiles = new List<ITrackedFile>();
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private int CabbingThreadCount { get; set; }

        private string IntermediateFolder { get; }

        private string LayoutDirectory { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        private string ModularizationSuffix { get; }

        private Dictionary<MediaSymbol, IEnumerable<IFileFacade>> FileFacadesByCabinet { get; }

        private WindowsInstallerData Data { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private Func<MediaSymbol, string, string, string> ResolveMedia { get; }

        public IEnumerable<IFileTransfer> FileTransfers => this.fileTransfers;

        public IEnumerable<ITrackedFile> TrackedFiles => this.trackedFiles;

        public void Execute()
        {
            var calculatedCabbingThreadCount = this.CalculateCabbingThreadCount();

            this.GetMediaTemplateAttributes(out var maximumCabinetSizeForLargeFileSplitting, out var maximumUncompressedMediaSize);

            var cabinetBuilder = new CabinetBuilder(this.Messaging, calculatedCabbingThreadCount, maximumCabinetSizeForLargeFileSplitting, maximumUncompressedMediaSize);

            var hashesByFileId = this.Section.Symbols.OfType<MsiFileHashSymbol>().ToDictionary(s => s.Id.Id);

            foreach (var entry in this.FileFacadesByCabinet)
            {
                var mediaSymbol = entry.Key;
                var files = entry.Value;
                var compressionLevel = mediaSymbol.CompressionLevel ?? this.DefaultCompressionLevel ?? CompressionLevel.Medium;
                var cabinetDir = this.ResolveMedia(mediaSymbol, mediaSymbol.Layout, this.LayoutDirectory);

                var cabinetWorkItem = this.CreateCabinetWorkItem(this.Data, cabinetDir, mediaSymbol, compressionLevel, files, hashesByFileId);
                if (null != cabinetWorkItem)
                {
                    cabinetBuilder.Enqueue(cabinetWorkItem);
                }
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Create queued cabinets with multiple threads.
            cabinetBuilder.CreateQueuedCabinets();

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            this.UpdateMediaWithSpannedCabinets(cabinetBuilder.CompletedCabinets);
        }

        private int CalculateCabbingThreadCount()
        {
            var processorCount = Environment.ProcessorCount;

            // If the number of processors is invalid, default to a single processor.
            if (processorCount == 0)
            {
                processorCount = 1;

                this.Messaging.Write(WarningMessages.InvalidEnvironmentVariable("NUMBER_OF_PROCESSORS", Environment.ProcessorCount.ToString(), processorCount.ToString()));
            }

            // If the cabbing thread count was provided, and it isn't more than double the number of processors, use it.
            if (this.CabbingThreadCount > 0 && processorCount < this.CabbingThreadCount * 2)
            {
                processorCount = this.CabbingThreadCount;
            }

            this.Messaging.Write(VerboseMessages.SetCabbingThreadCount(processorCount.ToString()));

            return processorCount;
        }

        private CabinetWorkItem CreateCabinetWorkItem(WindowsInstallerData data, string cabinetDir, MediaSymbol mediaSymbol, CompressionLevel compressionLevel, IEnumerable<IFileFacade> fileFacades, Dictionary<string, MsiFileHashSymbol> hashesByFileId)
        {
            CabinetWorkItem cabinetWorkItem = null;

            var intermediateCabinetPath = Path.Combine(this.IntermediateFolder, mediaSymbol.Cabinet);

            // check for an empty cabinet
            if (!fileFacades.Any())
            {
                // Remove the leading '#' from the embedded cabinet name to make the warning easier to understand
                var cabinetName = mediaSymbol.Cabinet.TrimStart('#');

                // If building a patch, remind them to run -p for torch.
                this.Messaging.Write(WarningMessages.EmptyCabinet(mediaSymbol.SourceLineNumbers, cabinetName, OutputType.Patch == data.Type));
            }

            var resolvedCabinet = this.cabinetResolver.ResolveCabinet(intermediateCabinetPath, fileFacades);

            // Create a cabinet work item if it's not being skipped.
            if (CabinetBuildOption.BuildAndCopy == resolvedCabinet.BuildOption || CabinetBuildOption.BuildAndMove == resolvedCabinet.BuildOption)
            {
                // Default to the threshold for best smartcabbing (makes smallest cabinet).
                cabinetWorkItem = new CabinetWorkItem(mediaSymbol.SourceLineNumbers, mediaSymbol.DiskId, resolvedCabinet.Path, fileFacades, hashesByFileId, maxThreshold: 0, compressionLevel: compressionLevel, modularizationSuffix: this.ModularizationSuffix);
            }
            else // reuse the cabinet from the cabinet cache.
            {
                this.Messaging.Write(VerboseMessages.ReusingCabCache(mediaSymbol.SourceLineNumbers, mediaSymbol.Cabinet, resolvedCabinet.Path));

                try
                {
                    // Ensure the cached cabinet timestamp is current to prevent perpetual incremental builds. The
                    // problematic scenario goes like this. Imagine two cabinets in the cache. Update a file that
                    // goes into one of the cabinets. One cabinet will get rebuilt, the other will be copied from
                    // the cache. Now the file (an input) has a newer timestamp than the reused cabient (an output)
                    // causing the project to look like it perpetually needs a rebuild until all of the reused
                    // cabinets get newer timestamps.
                    File.SetLastWriteTime(resolvedCabinet.Path, DateTime.Now);
                }
                catch (Exception e)
                {
                    this.Messaging.Write(WarningMessages.CannotUpdateCabCache(mediaSymbol.SourceLineNumbers, resolvedCabinet.Path, e.Message));
                }
            }

            var trackResolvedCabinet = this.BackendHelper.TrackFile(resolvedCabinet.Path, TrackedFileType.Intermediate, mediaSymbol.SourceLineNumbers);
            this.trackedFiles.Add(trackResolvedCabinet);

            if (mediaSymbol.Cabinet.StartsWith("#", StringComparison.Ordinal))
            {
                var streamsTable = data.EnsureTable(this.TableDefinitions["_Streams"]);

                var streamRow = streamsTable.CreateRow(mediaSymbol.SourceLineNumbers);
                streamRow[0] = mediaSymbol.Cabinet.Substring(1);
                streamRow[1] = resolvedCabinet.Path;
            }
            else
            {
                var trackDestination = this.BackendHelper.TrackFile(Path.Combine(cabinetDir, mediaSymbol.Cabinet), TrackedFileType.BuiltContentOutput, mediaSymbol.SourceLineNumbers);
                this.trackedFiles.Add(trackDestination);

                var transfer = this.BackendHelper.CreateFileTransfer(resolvedCabinet.Path, trackDestination.Path, resolvedCabinet.BuildOption == CabinetBuildOption.BuildAndMove, mediaSymbol.SourceLineNumbers);
                this.fileTransfers.Add(transfer);
            }

            return cabinetWorkItem;
        }

        /// <summary>
        /// Gets Compiler Values of MediaTemplate Attributes governing Maximum Cabinet Size after applying Environment Variable Overrides
        /// </summary>
        private void GetMediaTemplateAttributes(out int maxCabSizeForLargeFileSplitting, out int maxUncompressedMediaSize)
        {
            var mediaTemplate = this.Section.Symbols.OfType<WixMediaTemplateSymbol>().FirstOrDefault();

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            if (mediaTemplate != null)
            {
                // Get Environment Variable Overrides for MediaTemplate Attributes governing Maximum Cabinet Size
                var mcslfsString = Environment.GetEnvironmentVariable("WIX_MCSLFS");
                var mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");

                // Get the Value for Max Cab Size for File Splitting
                var maxCabSizeForLargeFileInMB = 0;
                try
                {
                    // Override authored mcslfs value if environment variable is authored.
                    maxCabSizeForLargeFileInMB = !String.IsNullOrEmpty(mcslfsString) ? Int32.Parse(mcslfsString) : mediaTemplate.MaximumCabinetSizeForLargeFileSplitting ?? MaxValueOfMaxCabSizeForLargeFileSplitting;

                    var testOverFlow = (ulong)maxCabSizeForLargeFileInMB * 1024 * 1024;
                    maxCabSizeForLargeFileSplitting = maxCabSizeForLargeFileInMB;
                }
                catch (FormatException)
                {
                    throw new WixException(ErrorMessages.IllegalEnvironmentVariable("WIX_MCSLFS", mcslfsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(ErrorMessages.MaximumCabinetSizeForLargeFileSplittingTooLarge(null, maxCabSizeForLargeFileInMB, MaxValueOfMaxCabSizeForLargeFileSplitting));
                }

                var maxPreCompressedSizeInMB = 0;
                try
                {
                    // Override authored mums value if environment variable is authored.
                    maxPreCompressedSizeInMB = !String.IsNullOrEmpty(mumsString) ? Int32.Parse(mumsString) : mediaTemplate.MaximumUncompressedMediaSize ?? DefaultMaximumUncompressedMediaSize;

                    var testOverFlow = (ulong)maxPreCompressedSizeInMB * 1024 * 1024;
                    maxUncompressedMediaSize = maxPreCompressedSizeInMB;
                }
                catch (FormatException)
                {
                    throw new WixException(ErrorMessages.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(ErrorMessages.MaximumUncompressedMediaSizeTooLarge(null, maxPreCompressedSizeInMB));
                }
            }
            else
            {
                maxCabSizeForLargeFileSplitting = 0;
                maxUncompressedMediaSize = DefaultMaximumUncompressedMediaSize;
            }
        }

        private void UpdateMediaWithSpannedCabinets(IReadOnlyCollection<CompletedCabinetWorkItem> completedCabinetWorkItems)
        {
            var completedCabinetsSpanned = completedCabinetWorkItems.Where(c => c.CreatedCabinets.Count > 1).OrderBy(c => c.DiskId).ToList();

            if (completedCabinetsSpanned.Count == 0)
            {
                return;
            }

            var fileTransfersByName = this.fileTransfers.ToDictionary(t => Path.GetFileName(t.Source), StringComparer.OrdinalIgnoreCase);
            var mediaTable = this.Data.Tables["Media"];
            var fileTable = this.Data.Tables["File"];
            var mediaRows = mediaTable.Rows.Cast<MediaRow>().OrderBy(m => m.DiskId).ToList();
            var fileRows = fileTable.Rows.Cast<FileRow>().OrderBy(f => f.Sequence).ToList();

            var mediaRowsByOriginalDiskId = mediaRows.ToDictionary(m => m.DiskId);
            var addedMediaRows = new List<MediaRow>();

            foreach (var completedCabinetSpanned in completedCabinetsSpanned)
            {
                var cabinet = completedCabinetSpanned.CreatedCabinets.First();
                var spannedCabinets = completedCabinetSpanned.CreatedCabinets.Skip(1);

                if (!fileTransfersByName.TryGetValue(cabinet.CabinetName, out var transfer) ||
                    !mediaRowsByOriginalDiskId.TryGetValue(completedCabinetSpanned.DiskId, out var mediaRow))
                {
                    throw new WixException(ErrorMessages.SplitCabinetCopyRegistrationFailed(spannedCabinets.First().CabinetName, cabinet.CabinetName));
                }

                var lastDiskId = mediaRow.DiskId;
                var mediaRowsThatWillNeedDiskIdUpdated = mediaRows.OrderBy(m => m.DiskId).Where(m => m.DiskId > mediaRow.DiskId).ToList();

                foreach (var spannedCabinet in spannedCabinets)
                {
                    var spannedCabinetSourcePath = Path.Combine(Path.GetDirectoryName(transfer.Source), spannedCabinet.CabinetName);
                    var spannedCabinetTargetPath = Path.Combine(Path.GetDirectoryName(transfer.Destination), spannedCabinet.CabinetName);

                    var trackSource = this.BackendHelper.TrackFile(spannedCabinetSourcePath, TrackedFileType.Intermediate, transfer.SourceLineNumbers);
                    this.trackedFiles.Add(trackSource);

                    var trackTarget = this.BackendHelper.TrackFile(spannedCabinetTargetPath, TrackedFileType.BuiltContentOutput, transfer.SourceLineNumbers);
                    this.trackedFiles.Add(trackTarget);

                    var newTransfer = this.BackendHelper.CreateFileTransfer(trackSource.Path, trackTarget.Path, transfer.Move, transfer.SourceLineNumbers);
                    this.fileTransfers.Add(newTransfer);

                    // FDI Extract requires DiskID of Split Cabinets to be continuous. So a new Media row must inserted just
                    // after the previous spanned cabinet according to DiskID sort order, otherwise Windows Installer will
                    // encounter Error 2350 (FDI Server Error).
                    var newMediaRow = (MediaRow)mediaTable.CreateRow(mediaRow.SourceLineNumbers);
                    newMediaRow.Cabinet = spannedCabinet.CabinetName;
                    newMediaRow.DiskId = ++lastDiskId;
                    newMediaRow.LastSequence = mediaRow.LastSequence;

                    addedMediaRows.Add(newMediaRow);
                }

                // Increment the DiskId for all Media rows that come after the newly inserted row to ensure that the DiskId is unique
                // and the Media rows stay in order based on last sequence.
                foreach (var updateMediaRow in mediaRowsThatWillNeedDiskIdUpdated)
                {
                    updateMediaRow.DiskId = ++lastDiskId;
                }
            }

            mediaTable.ValidateRows();

            var oldDiskIdToNewDiskId = mediaRowsByOriginalDiskId.Where(originalDiskIdWithMediaRow => originalDiskIdWithMediaRow.Value.DiskId != originalDiskIdWithMediaRow.Key)
                                                                .ToDictionary(originalDiskIdWithMediaRow => originalDiskIdWithMediaRow.Key, originalDiskIdWithMediaRow => originalDiskIdWithMediaRow.Value.DiskId);

            // Update the File row and FileSymbols so the DiskIds are correct in the WixOutput, even if this
            // data doesn't show up in the Windows Installer database.
            foreach (var fileRow in fileRows)
            {
                if (oldDiskIdToNewDiskId.TryGetValue(fileRow.DiskId, out var newDiskId))
                {
                    fileRow.DiskId = newDiskId;
                }
            }

            foreach (var fileSymbol in this.Section.Symbols.OfType<FileSymbol>())
            {
                if (fileSymbol.DiskId.HasValue && oldDiskIdToNewDiskId.TryGetValue(fileSymbol.DiskId.Value, out var newDiskId))
                {
                    fileSymbol.DiskId = newDiskId;
                }
            }

            // Update the MediaSymbol DiskIds to the correct DiskId. Note that the MediaSymbol Id
            // is not changed because symbol ids are not allowed to change after they are created.
            foreach (var mediaSymbol in this.Section.Symbols.OfType<MediaSymbol>())
            {
                if (oldDiskIdToNewDiskId.TryGetValue(mediaSymbol.DiskId, out var newDiskId))
                {
                    mediaSymbol.DiskId = newDiskId;
                }
            }

            // Now that the existing MediaSymbol DiskIds are updated, add the newly created Media rows
            // as symbols. Notice that the new MediaSymbols do not have an Id because they very likely
            // would conflict with MediaSymbols that had their DiskIds updated but Ids could not be updated.
            // The newly created MediaSymbols will rename anonymous.
            foreach (var mediaRow in addedMediaRows)
            {
                this.Section.AddSymbol(new MediaSymbol(mediaRow.SourceLineNumbers)
                {
                    Cabinet = mediaRow.Cabinet,
                    DiskId = mediaRow.DiskId,
                    DiskPrompt = mediaRow.DiskPrompt,
                    LastSequence = mediaRow.LastSequence,
                    Source = mediaRow.Source,
                    VolumeLabel = mediaRow.VolumeLabel
               });
            }
        }
    }
}
