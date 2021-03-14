// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Creates cabinet files.
    /// </summary>
    internal class CreateCabinetsCommand
    {
        public const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB
        public const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)

        private readonly List<IFileTransfer> fileTransfers;

        private readonly List<ITrackedFile> trackedFiles;

        private readonly FileSplitCabNamesCallback newCabNamesCallBack;

        private Dictionary<string, string> lastCabinetAddedToMediaTable; // Key is First Cabinet Name, Value is Last Cabinet Added in the Split Sequence

        public CreateCabinetsCommand(IWixToolsetServiceProvider serviceProvider, IBackendHelper backendHelper, WixMediaTemplateSymbol mediaTemplate)
        {
            this.fileTransfers = new List<IFileTransfer>();

            this.trackedFiles = new List<ITrackedFile>();

            this.newCabNamesCallBack = this.NewCabNamesCallBack;

            this.ServiceProvider = serviceProvider;

            this.BackendHelper = backendHelper;

            this.MediaTemplate = mediaTemplate;
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        private IBackendHelper BackendHelper { get; }

        private WixMediaTemplateSymbol MediaTemplate { get; }

        /// <summary>
        /// Sets the number of threads to use for cabinet creation.
        /// </summary>
        public int CabbingThreadCount { private get; set; }

        public string CabCachePath { private get; set; }

        public IMessaging Messaging { private get; set; }

        public string IntermediateFolder { private get; set; }

        /// <summary>
        /// Sets the default compression level to use for cabinets
        /// that don't have their compression level explicitly set.
        /// </summary>
        public CompressionLevel? DefaultCompressionLevel { private get; set; }

        public IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { private get; set; }

        public WindowsInstallerData Data { private get; set; }

        public string LayoutDirectory { private get; set; }

        public bool Compressed { private get; set; }

        public string ModularizationSuffix { private get; set; }

        public Dictionary<MediaSymbol, IEnumerable<IFileFacade>> FileFacadesByCabinet { private get; set; }

        public Func<MediaSymbol, string, string, string> ResolveMedia { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public IEnumerable<IFileTransfer> FileTransfers => this.fileTransfers;

        public IEnumerable<ITrackedFile> TrackedFiles => this.trackedFiles;

        public void Execute()
        {
            this.lastCabinetAddedToMediaTable = new Dictionary<string, string>();

            // If the cabbing thread count wasn't provided, default the number of cabbing threads to the number of processors.
            if (this.CabbingThreadCount <= 0)
            {
                this.CabbingThreadCount = this.CalculateCabbingThreadCount();
            }

            // Send Binder object to Facilitate NewCabNamesCallBack Callback
            var cabinetBuilder = new CabinetBuilder(this.Messaging, this.CabbingThreadCount, Marshal.GetFunctionPointerForDelegate(this.newCabNamesCallBack));

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            this.GetMediaTemplateAttributes(out var maximumCabinetSizeForLargeFileSplitting, out var maximumUncompressedMediaSize);
            cabinetBuilder.MaximumCabinetSizeForLargeFileSplitting = maximumCabinetSizeForLargeFileSplitting;
            cabinetBuilder.MaximumUncompressedMediaSize = maximumUncompressedMediaSize;

            foreach (var entry in this.FileFacadesByCabinet)
            {
                var mediaSymbol = entry.Key;
                var files = entry.Value;
                var compressionLevel = mediaSymbol.CompressionLevel ?? this.DefaultCompressionLevel ?? CompressionLevel.Medium;
                var cabinetDir = this.ResolveMedia(mediaSymbol, mediaSymbol.Layout, this.LayoutDirectory);

                var cabinetWorkItem = this.CreateCabinetWorkItem(this.Data, cabinetDir, mediaSymbol, compressionLevel, files);
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

            // create queued cabinets with multiple threads
            cabinetBuilder.CreateQueuedCabinets();
            if (this.Messaging.EncounteredError)
            {
                return;
            }
        }

        private int CalculateCabbingThreadCount()
        {
            var cabbingThreadCount = 1;  // default to 1 if the environment variable is not set.

            var numberOfProcessors = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");

            try
            {
                if (!String.IsNullOrEmpty(numberOfProcessors))
                {
                    cabbingThreadCount = Convert.ToInt32(numberOfProcessors, CultureInfo.InvariantCulture.NumberFormat);

                    if (cabbingThreadCount <= 0)
                    {
                        throw new WixException(ErrorMessages.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                    }
                }

                this.Messaging.Write(VerboseMessages.SetCabbingThreadCount(this.CabbingThreadCount.ToString()));
            }
            catch (ArgumentException)
            {
                throw new WixException(ErrorMessages.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
            }
            catch (FormatException)
            {
                throw new WixException(ErrorMessages.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
            }

            return cabbingThreadCount;
        }

        /// <summary>
        /// Creates a work item to create a cabinet.
        /// </summary>
        /// <param name="data">Windows Installer data for the current database.</param>
        /// <param name="cabinetDir">Directory to create cabinet in.</param>
        /// <param name="mediaSymbol">Media symbol containing information about the cabinet.</param>
        /// <param name="compressionLevel">Desired compression level.</param>
        /// <param name="fileFacades">Collection of files in this cabinet.</param>
        /// <returns>created CabinetWorkItem object</returns>
        private CabinetWorkItem CreateCabinetWorkItem(WindowsInstallerData data, string cabinetDir, MediaSymbol mediaSymbol, CompressionLevel compressionLevel, IEnumerable<IFileFacade> fileFacades)
        {
            CabinetWorkItem cabinetWorkItem = null;
            var tempCabinetFileX = Path.Combine(this.IntermediateFolder, mediaSymbol.Cabinet);

            // check for an empty cabinet
            if (!fileFacades.Any())
            {
                // Remove the leading '#' from the embedded cabinet name to make the warning easier to understand
                var cabinetName = mediaSymbol.Cabinet.TrimStart('#');

                // If building a patch, remind them to run -p for torch.
                if (OutputType.Patch == data.Type)
                {
                    this.Messaging.Write(WarningMessages.EmptyCabinet(mediaSymbol.SourceLineNumbers, cabinetName, true));
                }
                else
                {
                    this.Messaging.Write(WarningMessages.EmptyCabinet(mediaSymbol.SourceLineNumbers, cabinetName));
                }
            }

            var cabinetResolver = new CabinetResolver(this.ServiceProvider, this.CabCachePath, this.BackendExtensions);

            var resolvedCabinet = cabinetResolver.ResolveCabinet(tempCabinetFileX, fileFacades);

            // create a cabinet work item if it's not being skipped
            if (CabinetBuildOption.BuildAndCopy == resolvedCabinet.BuildOption || CabinetBuildOption.BuildAndMove == resolvedCabinet.BuildOption)
            {
                // Default to the threshold for best smartcabbing (makes smallest cabinet).
                cabinetWorkItem = new CabinetWorkItem(fileFacades, resolvedCabinet.Path, maxThreshold: 0, compressionLevel, this.ModularizationSuffix /*, this.FileManager*/);
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
                var trackDestination = this.BackendHelper.TrackFile(Path.Combine(cabinetDir, mediaSymbol.Cabinet), TrackedFileType.Final, mediaSymbol.SourceLineNumbers);
                this.trackedFiles.Add(trackDestination);

                var transfer = this.BackendHelper.CreateFileTransfer(resolvedCabinet.Path, trackDestination.Path, resolvedCabinet.BuildOption == CabinetBuildOption.BuildAndMove, mediaSymbol.SourceLineNumbers);
                this.fileTransfers.Add(transfer);
            }

            return cabinetWorkItem;
        }

        //private ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<FileFacade> fileFacades)
        //{
        //    ResolvedCabinet resolved = null;

        //    List<BindFileWithPath> filesWithPath = fileFacades.Select(f => new BindFileWithPath() { Id = f.File.File, Path = f.WixFile.Source }).ToList();

        //    foreach (var extension in this.BackendExtensions)
        //    {
        //        resolved = extension.ResolveCabinet(cabinetPath, filesWithPath);
        //        if (null != resolved)
        //        {
        //            break;
        //        }
        //    }

        //    return resolved;
        //}

        /// <summary>
        /// Delegate for Cabinet Split Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void FileSplitCabNamesCallback([MarshalAs(UnmanagedType.LPWStr)]string firstCabName, [MarshalAs(UnmanagedType.LPWStr)]string newCabName, [MarshalAs(UnmanagedType.LPWStr)]string fileToken);

        /// <summary>
        /// Call back to Add File Transfer for new Cab and add new Cab to Media table
        /// This callback can come from Multiple Cabinet Builder Threads and so should be thread safe
        /// This callback will not be called in case there is no File splitting. i.e. MaximumCabinetSizeForLargeFileSplitting was not authored
        /// </summary>
        /// <param name="firstCabName">The name of splitting cabinet without extention e.g. "cab1".</param>
        /// <param name="newCabinetName">The name of the new cabinet that would be formed by splitting e.g. "cab1b.cab"</param>
        /// <param name="fileToken">The file token of the first file present in the splitting cabinet</param>
        internal void NewCabNamesCallBack([MarshalAs(UnmanagedType.LPWStr)]string firstCabName, [MarshalAs(UnmanagedType.LPWStr)]string newCabinetName, [MarshalAs(UnmanagedType.LPWStr)]string fileToken)
        {
            throw new NotImplementedException();
#if TODO_CAB_SPANNING
            // Locking Mutex here as this callback can come from Multiple Cabinet Builder Threads
            var mutex = new Mutex(false, "WixCabinetSplitBinderCallback");
            try
            {
                if (!mutex.WaitOne(0, false)) // Check if you can get the lock
                {
                    // Cound not get the Lock
                    this.Messaging.Write(VerboseMessages.CabinetsSplitInParallel());
                    mutex.WaitOne(); // Wait on other thread
                }

                var firstCabinetName = firstCabName + ".cab";
                var transferAdded = false; // Used for Error Handling

                // Create File Transfer for new Cabinet using transfer of Base Cabinet
                foreach (var transfer in this.FileTransfers)
                {
                    if (firstCabinetName.Equals(Path.GetFileName(transfer.Source), StringComparison.InvariantCultureIgnoreCase))
                    {
                        var newCabSourcePath = Path.Combine(Path.GetDirectoryName(transfer.Source), newCabinetName);
                        var newCabTargetPath = Path.Combine(Path.GetDirectoryName(transfer.Destination), newCabinetName);

                        var trackSource = this.BackendHelper.TrackFile(newCabSourcePath, TrackedFileType.Intermediate, transfer.SourceLineNumbers);
                        this.trackedFiles.Add(trackSource);

                        var trackTarget = this.BackendHelper.TrackFile(newCabTargetPath, TrackedFileType.Final, transfer.SourceLineNumbers);
                        this.trackedFiles.Add(trackTarget);

                        var newTransfer = this.BackendHelper.CreateFileTransfer(trackSource.Path, trackTarget.Path, transfer.Move, transfer.SourceLineNumbers);
                        this.fileTransfers.Add(newTransfer);

                        transferAdded = true;
                        break;
                    }
                }

                // Check if File Transfer was added
                if (!transferAdded)
                {
                    throw new WixException(ErrorMessages.SplitCabinetCopyRegistrationFailed(newCabinetName, firstCabinetName));
                }

                // Add the new Cabinets to media table using LastSequence of Base Cabinet
                var mediaTable = this.Output.Tables["Media"];
                var wixFileTable = this.Output.Tables["WixFile"];
                var diskIDForLastSplitCabAdded = 0; // The DiskID value for the first cab in this cabinet split chain
                var lastSequenceForLastSplitCabAdded = 0; // The LastSequence value for the first cab in this cabinet split chain
                var lastSplitCabinetFound = false; // Used for Error Handling

                var lastCabinetOfThisSequence = String.Empty;
                // Get the Value of Last Cabinet Added in this split Sequence from Dictionary
                if (!this.lastCabinetAddedToMediaTable.TryGetValue(firstCabinetName, out lastCabinetOfThisSequence))
                {
                    // If there is no value for this sequence, then use first Cabinet is the last one of this split sequence
                    lastCabinetOfThisSequence = firstCabinetName;
                }

                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    // Get details for the Last Cabinet Added in this Split Sequence
                    if ((lastSequenceForLastSplitCabAdded == 0) && lastCabinetOfThisSequence.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        lastSequenceForLastSplitCabAdded = mediaRow.LastSequence;
                        diskIDForLastSplitCabAdded = mediaRow.DiskId;
                        lastSplitCabinetFound = true;
                    }

                    // Check for Name Collision for the new Cabinet added
                    if (newCabinetName.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Name Collision of generated Split Cabinet Name and user Specified Cab name for current row
                        throw new WixException(ErrorMessages.SplitCabinetNameCollision(newCabinetName, firstCabinetName));
                    }
                }

                // Check if the last Split Cabinet was found in the Media Table
                if (!lastSplitCabinetFound)
                {
                    throw new WixException(ErrorMessages.SplitCabinetInsertionFailed(newCabinetName, firstCabinetName, lastCabinetOfThisSequence));
                }

                // The new Row has to be inserted just after the last cab in this cabinet split chain according to DiskID Sort
                // This is because the FDI Extract requires DiskID of Split Cabinets to be continuous. It Fails otherwise with
                // Error 2350 (FDI Server Error) as next DiskID did not have the right split cabinet during extraction
                MediaRow newMediaRow = (MediaRow)mediaTable.CreateRow(null);
                newMediaRow.Cabinet = newCabinetName;
                newMediaRow.DiskId = diskIDForLastSplitCabAdded + 1; // When Sorted with DiskID, this new Cabinet Row is an Insertion
                newMediaRow.LastSequence = lastSequenceForLastSplitCabAdded;

                // Now increment the DiskID for all rows that come after the newly inserted row to Ensure that DiskId is unique
                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    // Check if this row comes after inserted row and it is not the new cabinet inserted row
                    if (mediaRow.DiskId >= newMediaRow.DiskId && !newCabinetName.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        mediaRow.DiskId++; // Increment DiskID
                    }
                }

                // Now Increment DiskID for All files Rows so that they refer to the right Media Row
                foreach (WixFileRow wixFileRow in wixFileTable.Rows)
                {
                    // Check if this row comes after inserted row and if this row is not the file that has to go into the current cabinet
                    // This check will work as we have only one large file in every splitting cabinet
                    // If we want to support splitting cabinet with more large files we need to update this code
                    if (wixFileRow.DiskId >= newMediaRow.DiskId && !wixFileRow.File.Equals(fileToken, StringComparison.InvariantCultureIgnoreCase))
                    {
                        wixFileRow.DiskId++; // Increment DiskID
                    }
                }

                // Update the Last Cabinet Added in the Split Sequence in Dictionary for future callback
                this.lastCabinetAddedToMediaTable[firstCabinetName] = newCabinetName;

                mediaTable.ValidateRows(); // Valdiates DiskDIs, throws Exception as Wix Error if validation fails
            }
            finally
            {
                // Releasing the Mutex here
                mutex.ReleaseMutex();
            }
#endif
        }


        /// <summary>
        /// Gets Compiler Values of MediaTemplate Attributes governing Maximum Cabinet Size after applying Environment Variable Overrides
        /// </summary>
        private void GetMediaTemplateAttributes(out int maxCabSizeForLargeFileSplitting, out int maxUncompressedMediaSize)
        {
            // Get Environment Variable Overrides for MediaTemplate Attributes governing Maximum Cabinet Size
            var mcslfsString = Environment.GetEnvironmentVariable("WIX_MCSLFS");
            var mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            if (this.MediaTemplate != null)
            {
                // Get the Value for Max Cab Size for File Splitting
                var maxCabSizeForLargeFileInMB = 0;
                try
                {
                    // Override authored mcslfs value if environment variable is authored.
                    maxCabSizeForLargeFileInMB = !String.IsNullOrEmpty(mcslfsString) ? Int32.Parse(mcslfsString) : this.MediaTemplate.MaximumCabinetSizeForLargeFileSplitting ?? MaxValueOfMaxCabSizeForLargeFileSplitting;

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
                    maxPreCompressedSizeInMB = !String.IsNullOrEmpty(mumsString) ? Int32.Parse(mumsString) : this.MediaTemplate.MaximumUncompressedMediaSize ?? DefaultMaximumUncompressedMediaSize;

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
    }
}
