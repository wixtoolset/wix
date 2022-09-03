// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Builds cabinets using multiple threads. This implements a thread pool that generates cabinets with multiple
    /// threads. Unlike System.Threading.ThreadPool, it waits until all threads are finished.
    /// </summary>
    internal sealed class CabinetBuilder
    {
        private readonly Queue<CabinetWorkItem> cabinetWorkItems;
        private readonly List<CompletedCabinetWorkItem> completedCabinets;

        public CabinetBuilder(IMessaging messaging, int threadCount, int maximumCabinetSizeForLargeFileSplitting, int maximumUncompressedMediaSize)
        {
            if (0 >= threadCount)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCount));
            }

            this.cabinetWorkItems = new Queue<CabinetWorkItem>();
            this.completedCabinets = new List<CompletedCabinetWorkItem>();

            this.Messaging = messaging;
            this.ThreadCount = threadCount;
            this.MaximumCabinetSizeForLargeFileSplitting = maximumCabinetSizeForLargeFileSplitting;
            this.MaximumUncompressedMediaSize = maximumUncompressedMediaSize;
        }

        private IMessaging Messaging { get; }

        private int ThreadCount { get; }

        private int MaximumCabinetSizeForLargeFileSplitting { get; }

        private int MaximumUncompressedMediaSize { get; }

        public IReadOnlyCollection<CompletedCabinetWorkItem> CompletedCabinets => this.completedCabinets;

        /// <summary>
        /// Enqueues a CabinetWorkItem to the queue.
        /// </summary>
        /// <param name="cabinetWorkItem">cabinet work item</param>
        public void Enqueue(CabinetWorkItem cabinetWorkItem)
        {
            this.cabinetWorkItems.Enqueue(cabinetWorkItem);
        }

        /// <summary>
        /// Create the queued cabinets.
        /// </summary>
        /// <returns>error message number (zero if no error)</returns>
        public void CreateQueuedCabinets()
        {
            if (this.cabinetWorkItems.Count == 0)
            {
                return;
            }

            var cabinetFolders = this.cabinetWorkItems.Select(c => Path.GetDirectoryName(c.CabinetFile)).Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in cabinetFolders)
            {
                Directory.CreateDirectory(folder);
            }

            // don't create more threads than the number of cabinets to build
            var numberOfThreads = Math.Min(this.ThreadCount, this.cabinetWorkItems.Count);

            if (0 < numberOfThreads)
            {
                var threads = new Thread[numberOfThreads];

                for (var i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(new ThreadStart(this.ProcessWorkItems));
                    threads[i].Start();
                }

                // wait for all threads to finish
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
        }

        /// <summary>
        /// This function gets called by multiple threads to do actual work.
        /// It takes one work item at a time and calls this.CreateCabinet().
        /// It does not return until cabinetWorkItems queue is empty
        /// </summary>
        private void ProcessWorkItems()
        {
            try
            {
                while (true)
                {
                    CabinetWorkItem cabinetWorkItem;

                    lock (this.cabinetWorkItems)
                    {
                        // check if there are any more cabinets to create
                        if (0 == this.cabinetWorkItems.Count)
                        {
                            break;
                        }

                        cabinetWorkItem = this.cabinetWorkItems.Dequeue();
                    }

                    // Create a cabinet.
                    var created = this.CreateCabinet(cabinetWorkItem);

                    // Update the cabinet work item to report back what cabinets were created.
                    lock (this.completedCabinets)
                    {
                        this.completedCabinets.Add(new CompletedCabinetWorkItem(cabinetWorkItem.DiskId, created));
                    }
                }
            }
            catch (WixException we)
            {
                this.Messaging.Write(we.Error);
            }
            catch (Exception e)
            {
                this.Messaging.Write(ErrorMessages.UnexpectedException(e));
            }
        }

        /// <summary>
        /// Creates a cabinet using the wixcab.dll interop layer.
        /// </summary>
        /// <param name="cabinetWorkItem">CabinetWorkItem containing information about the cabinet to create.</param>
        private IReadOnlyCollection<CabinetCreated> CreateCabinet(CabinetWorkItem cabinetWorkItem)
        {
            this.Messaging.Write(VerboseMessages.CreateCabinet(cabinetWorkItem.CabinetFile));

            var maxCabinetSize = 0; // The value of 0 corresponds to default of 2GB which means no cabinet splitting
            ulong maxPreCompressedSizeInBytes = 0;

            if (this.MaximumCabinetSizeForLargeFileSplitting != 0)
            {
                // User Specified Max Cab Size for File Splitting, So Check if this cabinet has a single file larger than MaximumUncompressedFileSize
                // If a file is larger than MaximumUncompressedFileSize, then the cabinet containing it will have only this file
                if (1 == cabinetWorkItem.FileFacades.Count())
                {
                    // Cabinet has Single File, Check if this is Large File than needs Splitting into Multiple cabs
                    // Get the Value for Max Uncompressed Media Size
                    maxPreCompressedSizeInBytes = (ulong)this.MaximumUncompressedMediaSize * 1024 * 1024;

                    var facade = cabinetWorkItem.FileFacades.First();

                    // If the file is larger than MaximumUncompressedFileSize set Maximum Cabinet Size for Cabinet Splitting
                    if ((ulong)facade.FileSize >= maxPreCompressedSizeInBytes)
                    {
                        maxCabinetSize = this.MaximumCabinetSizeForLargeFileSplitting;
                    }
                }
            }

            // Calculate the files to be compressed into the cabinet.
            var compressFiles = new List<CabinetCompressFile>();

            foreach (var facade in cabinetWorkItem.FileFacades.OrderBy(f => f.Sequence))
            {
                var modularizedId = facade.Id + cabinetWorkItem.ModularizationSuffix;

                var compressFile = cabinetWorkItem.HashesByFileId.TryGetValue(facade.Id, out var hash) ?
                    new CabinetCompressFile(facade.SourcePath, modularizedId, hash.HashPart1, hash.HashPart2, hash.HashPart3, hash.HashPart4) :
                    new CabinetCompressFile(facade.SourcePath, modularizedId);

                compressFiles.Add(compressFile);
            }

            // create the cabinet file
            var cabinetPath = Path.GetFullPath(cabinetWorkItem.CabinetFile);
            var cab = new Cabinet(cabinetPath);
            var created = cab.Compress(compressFiles, cabinetWorkItem.CompressionLevel, maxCabinetSize, cabinetWorkItem.MaxThreshold);

            // Best effort check to see if the cabinet is too large for the Windows Installer.
            try
            {
                var fi = new FileInfo(cabinetPath);
                if (fi.Length > Int32.MaxValue)
                {
                    this.Messaging.Write(WarningMessages.WindowsInstallerFileTooLarge(cabinetWorkItem.SourceLineNumber, cabinetPath, "cabinet"));
                }
            }
            catch
            {
            }

            return created;
        }
    }
}
