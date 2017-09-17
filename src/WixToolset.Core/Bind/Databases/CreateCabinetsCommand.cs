// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// Creates cabinet files.
    /// </summary>
    internal class CreateCabinetsCommand : ICommand
    {
        private List<FileTransfer> fileTransfers;

        private FileSplitCabNamesCallback newCabNamesCallBack;

        private Dictionary<string, string> lastCabinetAddedToMediaTable; // Key is First Cabinet Name, Value is Last Cabinet Added in the Split Sequence

        public CreateCabinetsCommand()
        {
            this.fileTransfers = new List<FileTransfer>();

            this.newCabNamesCallBack = NewCabNamesCallBack;
        }

        /// <summary>
        /// Sets the number of threads to use for cabinet creation.
        /// </summary>
        public int CabbingThreadCount { private get; set; }

        public string TempFilesLocation { private get; set; }

        /// <summary>
        /// Sets the default compression level to use for cabinets
        /// that don't have their compression level explicitly set.
        /// </summary>
        public CompressionLevel DefaultCompressionLevel { private get; set; }

        public Output Output { private get; set; }

        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public string LayoutDirectory { private get; set; }

        public bool Compressed { private get; set; }

        public Dictionary<MediaRow, IEnumerable<FileFacade>> FileRowsByCabinet { private get; set; }

        public Func<MediaRow, string, string, string> ResolveMedia { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public Table WixMediaTable { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get { return this.fileTransfers; } }

        /// <param name="output">Output to generate image for.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <param name="layoutDirectory">The directory in which the image should be layed out.</param>
        /// <param name="compressed">Flag if source image should be compressed.</param>
        /// <returns>The uncompressed file rows.</returns>
        public void Execute()
        {
            RowDictionary<WixMediaRow> wixMediaRows = new RowDictionary<WixMediaRow>(this.WixMediaTable);

            this.lastCabinetAddedToMediaTable = new Dictionary<string, string>();

            this.SetCabbingThreadCount();

            // Send Binder object to Facilitate NewCabNamesCallBack Callback
            CabinetBuilder cabinetBuilder = new CabinetBuilder(this.CabbingThreadCount, Marshal.GetFunctionPointerForDelegate(this.newCabNamesCallBack));

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            int MaximumCabinetSizeForLargeFileSplitting;
            int MaximumUncompressedMediaSize;
            this.GetMediaTemplateAttributes(out MaximumCabinetSizeForLargeFileSplitting, out MaximumUncompressedMediaSize);
            cabinetBuilder.MaximumCabinetSizeForLargeFileSplitting = MaximumCabinetSizeForLargeFileSplitting;
            cabinetBuilder.MaximumUncompressedMediaSize = MaximumUncompressedMediaSize;

            foreach (var entry in this.FileRowsByCabinet)
            {
                MediaRow mediaRow = entry.Key;
                IEnumerable<FileFacade> files = entry.Value;
                CompressionLevel compressionLevel = this.DefaultCompressionLevel;

                WixMediaRow wixMediaRow = null;
                string mediaLayoutFolder = null;

                if (wixMediaRows.TryGetValue(mediaRow.GetKey(), out wixMediaRow))
                {
                    mediaLayoutFolder = wixMediaRow.Layout;

                    if (wixMediaRow.CompressionLevel.HasValue)
                    {
                        compressionLevel = wixMediaRow.CompressionLevel.Value;
                    }
                }

                string cabinetDir = this.ResolveMedia(mediaRow, mediaLayoutFolder, this.LayoutDirectory);

                CabinetWorkItem cabinetWorkItem = this.CreateCabinetWorkItem(this.Output, cabinetDir, mediaRow, compressionLevel, files, this.fileTransfers);
                if (null != cabinetWorkItem)
                {
                    cabinetBuilder.Enqueue(cabinetWorkItem);
                }
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // create queued cabinets with multiple threads
            cabinetBuilder.CreateQueuedCabinets();
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }
        }

        /// <summary>
        /// Sets the thead count to the number of processors if the current thread count is set to 0.
        /// </summary>
        /// <remarks>The thread count value must be greater than 0 otherwise and exception will be thrown.</remarks>
        private void SetCabbingThreadCount()
        {
            // default the number of cabbing threads to the number of processors if it wasn't specified
            if (0 == this.CabbingThreadCount)
            {
                string numberOfProcessors = System.Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");

                try
                {
                    if (null != numberOfProcessors)
                    {
                        this.CabbingThreadCount = Convert.ToInt32(numberOfProcessors, CultureInfo.InvariantCulture.NumberFormat);

                        if (0 >= this.CabbingThreadCount)
                        {
                            throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                        }
                    }
                    else // default to 1 if the environment variable is not set
                    {
                        this.CabbingThreadCount = 1;
                    }

                    Messaging.Instance.OnMessage(WixVerboses.SetCabbingThreadCount(this.CabbingThreadCount.ToString()));
                }
                catch (ArgumentException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                }
            }
        }


        /// <summary>
        /// Creates a work item to create a cabinet.
        /// </summary>
        /// <param name="output">Output for the current database.</param>
        /// <param name="cabinetDir">Directory to create cabinet in.</param>
        /// <param name="mediaRow">MediaRow containing information about the cabinet.</param>
        /// <param name="fileFacades">Collection of files in this cabinet.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <returns>created CabinetWorkItem object</returns>
        private CabinetWorkItem CreateCabinetWorkItem(Output output, string cabinetDir, MediaRow mediaRow, CompressionLevel compressionLevel, IEnumerable<FileFacade> fileFacades, List<FileTransfer> fileTransfers)
        {
            CabinetWorkItem cabinetWorkItem = null;
            string tempCabinetFileX = Path.Combine(this.TempFilesLocation, mediaRow.Cabinet);

            // check for an empty cabinet
            if (!fileFacades.Any())
            {
                string cabinetName = mediaRow.Cabinet;

                // remove the leading '#' from the embedded cabinet name to make the warning easier to understand
                if (cabinetName.StartsWith("#", StringComparison.Ordinal))
                {
                    cabinetName = cabinetName.Substring(1);
                }

                // If building a patch, remind them to run -p for torch.
                if (OutputType.Patch == output.Type)
                {
                    Messaging.Instance.OnMessage(WixWarnings.EmptyCabinet(mediaRow.SourceLineNumbers, cabinetName, true));
                }
                else
                {
                    Messaging.Instance.OnMessage(WixWarnings.EmptyCabinet(mediaRow.SourceLineNumbers, cabinetName));
                }
            }

            ResolvedCabinet resolvedCabinet = this.ResolveCabinet(tempCabinetFileX, fileFacades);

            // create a cabinet work item if it's not being skipped
            if (CabinetBuildOption.BuildAndCopy == resolvedCabinet.BuildOption || CabinetBuildOption.BuildAndMove == resolvedCabinet.BuildOption)
            {
                int maxThreshold = 0; // default to the threshold for best smartcabbing (makes smallest cabinet).

                cabinetWorkItem = new CabinetWorkItem(fileFacades, resolvedCabinet.Path, maxThreshold, compressionLevel/*, this.FileManager*/);
            }
            else // reuse the cabinet from the cabinet cache.
            {
                Messaging.Instance.OnMessage(WixVerboses.ReusingCabCache(mediaRow.SourceLineNumbers, mediaRow.Cabinet, resolvedCabinet.Path));

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
                    Messaging.Instance.OnMessage(WixWarnings.CannotUpdateCabCache(mediaRow.SourceLineNumbers, resolvedCabinet.Path, e.Message));
                }
            }

            if (mediaRow.Cabinet.StartsWith("#", StringComparison.Ordinal))
            {
                Table streamsTable = output.EnsureTable(this.TableDefinitions["_Streams"]);

                Row streamRow = streamsTable.CreateRow(mediaRow.SourceLineNumbers);
                streamRow[0] = mediaRow.Cabinet.Substring(1);
                streamRow[1] = resolvedCabinet.Path;
            }
            else
            {
                string destinationPath = Path.Combine(cabinetDir, mediaRow.Cabinet);
                FileTransfer transfer;
                if (FileTransfer.TryCreate(resolvedCabinet.Path, destinationPath, CabinetBuildOption.BuildAndMove == resolvedCabinet.BuildOption, "Cabinet", mediaRow.SourceLineNumbers, out transfer))
                {
                    transfer.Built = true;
                    fileTransfers.Add(transfer);
                }
            }

            return cabinetWorkItem;
        }

        private ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<FileFacade> fileFacades)
        {
            ResolvedCabinet resolved = null;

            List<BindFileWithPath> filesWithPath = fileFacades.Select(f => new BindFileWithPath() { Id = f.File.File, Path = f.WixFile.Source }).ToList();

            foreach (IBinderFileManager fileManager in this.FileManagers)
            {
                resolved = fileManager.ResolveCabinet(cabinetPath, filesWithPath);
                if (null != resolved)
                {
                    break;
                }
            }

            return resolved;
        }

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
        /// <param name="newCabName">The name of the new cabinet that would be formed by splitting e.g. "cab1b.cab"</param>
        /// <param name="fileToken">The file token of the first file present in the splitting cabinet</param>
        internal void NewCabNamesCallBack([MarshalAs(UnmanagedType.LPWStr)]string firstCabName, [MarshalAs(UnmanagedType.LPWStr)]string newCabName, [MarshalAs(UnmanagedType.LPWStr)]string fileToken)
        {
            // Locking Mutex here as this callback can come from Multiple Cabinet Builder Threads
            Mutex mutex = new Mutex(false, "WixCabinetSplitBinderCallback");
            try
            {
                if (!mutex.WaitOne(0, false)) // Check if you can get the lock
                {
                    // Cound not get the Lock
                    Messaging.Instance.OnMessage(WixVerboses.CabinetsSplitInParallel());
                    mutex.WaitOne(); // Wait on other thread
                }

                string firstCabinetName = firstCabName + ".cab";
                string newCabinetName = newCabName;
                bool transferAdded = false; // Used for Error Handling

                // Create File Transfer for new Cabinet using transfer of Base Cabinet
                foreach (FileTransfer transfer in this.FileTransfers)
                {
                    if (firstCabinetName.Equals(Path.GetFileName(transfer.Source), StringComparison.InvariantCultureIgnoreCase))
                    {
                        string newCabSourcePath = Path.Combine(Path.GetDirectoryName(transfer.Source), newCabinetName);
                        string newCabTargetPath = Path.Combine(Path.GetDirectoryName(transfer.Destination), newCabinetName);

                        FileTransfer newTransfer;
                        if (FileTransfer.TryCreate(newCabSourcePath, newCabTargetPath, transfer.Move, "Cabinet", transfer.SourceLineNumbers, out newTransfer))
                        {
                            newTransfer.Built = true;
                            this.fileTransfers.Add(newTransfer);
                            transferAdded = true;
                            break;
                        }
                    }
                }

                // Check if File Transfer was added
                if (!transferAdded)
                {
                    throw new WixException(WixErrors.SplitCabinetCopyRegistrationFailed(newCabinetName, firstCabinetName));
                }

                // Add the new Cabinets to media table using LastSequence of Base Cabinet
                Table mediaTable = this.Output.Tables["Media"];
                Table wixFileTable = this.Output.Tables["WixFile"];
                int diskIDForLastSplitCabAdded = 0; // The DiskID value for the first cab in this cabinet split chain
                int lastSequenceForLastSplitCabAdded = 0; // The LastSequence value for the first cab in this cabinet split chain
                bool lastSplitCabinetFound = false; // Used for Error Handling

                string lastCabinetOfThisSequence = String.Empty;
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
                        throw new WixException(WixErrors.SplitCabinetNameCollision(newCabinetName, firstCabinetName));
                    }
                }

                // Check if the last Split Cabinet was found in the Media Table
                if (!lastSplitCabinetFound)
                {
                    throw new WixException(WixErrors.SplitCabinetInsertionFailed(newCabinetName, firstCabinetName, lastCabinetOfThisSequence));
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
        }


        /// <summary>
        /// Gets Compiler Values of MediaTemplate Attributes governing Maximum Cabinet Size after applying Environment Variable Overrides
        /// </summary>
        /// <param name="output">Output to generate image for.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        private void GetMediaTemplateAttributes(out int maxCabSizeForLargeFileSplitting, out int maxUncompressedMediaSize)
        {
            // Get Environment Variable Overrides for MediaTemplate Attributes governing Maximum Cabinet Size
            string mcslfsString = Environment.GetEnvironmentVariable("WIX_MCSLFS");
            string mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");
            int maxCabSizeForLargeFileInMB = 0;
            int maxPreCompressedSizeInMB = 0;
            ulong testOverFlow = 0;

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            Table mediaTemplateTable = this.Output.Tables["WixMediaTemplate"];
            if (mediaTemplateTable != null)
            {
                WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)mediaTemplateTable.Rows[0];

                // Get the Value for Max Cab Size for File Splitting
                try
                {
                    // Override authored mcslfs value if environment variable is authored.
                    if (!String.IsNullOrEmpty(mcslfsString))
                    {
                        maxCabSizeForLargeFileInMB = Int32.Parse(mcslfsString);
                    }
                    else
                    {
                        maxCabSizeForLargeFileInMB = mediaTemplateRow.MaximumCabinetSizeForLargeFileSplitting;
                    }
                    testOverFlow = (ulong)maxCabSizeForLargeFileInMB * 1024 * 1024;
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MCSLFS", mcslfsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(WixErrors.MaximumCabinetSizeForLargeFileSplittingTooLarge(null, maxCabSizeForLargeFileInMB, CompilerCore.MaxValueOfMaxCabSizeForLargeFileSplitting));
                }

                try
                {
                    // Override authored mums value if environment variable is authored.
                    if (!String.IsNullOrEmpty(mumsString))
                    {
                        maxPreCompressedSizeInMB = Int32.Parse(mumsString);
                    }
                    else
                    {
                        maxPreCompressedSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize;
                    }
                    testOverFlow = (ulong)maxPreCompressedSizeInMB * 1024 * 1024;
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(WixErrors.MaximumUncompressedMediaSizeTooLarge(null, maxPreCompressedSizeInMB));
                }

                maxCabSizeForLargeFileSplitting = maxCabSizeForLargeFileInMB;
                maxUncompressedMediaSize = maxPreCompressedSizeInMB;
            }
            else
            {
                maxCabSizeForLargeFileSplitting = 0;
                maxUncompressedMediaSize = CompilerCore.DefaultMaximumUncompressedMediaSize;
            }
        }
    }
}
