// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Builds cabinets using multiple threads. This implements a thread pool that generates cabinets with multiple
    /// threads. Unlike System.Threading.ThreadPool, it waits until all threads are finished.
    /// </summary>
    internal sealed class CabinetBuilder
    {
        private Queue cabinetWorkItems;
        private object lockObject;
        private int threadCount;

        // Address of Binder's callback function for Cabinet Splitting
        private IntPtr newCabNamesCallBackAddress;

        /// <summary>
        /// Instantiate a new CabinetBuilder.
        /// </summary>
        /// <param name="threadCount">number of threads to use</param>
        /// <param name="newCabNamesCallBackAddress">Address of Binder's callback function for Cabinet Splitting</param>
        public CabinetBuilder(IMessaging messaging, int threadCount, IntPtr newCabNamesCallBackAddress)
        {
            if (0 >= threadCount)
            {
                throw new ArgumentOutOfRangeException("threadCount");
            }

            this.cabinetWorkItems = new Queue();
            this.lockObject = new object();
            this.Messaging = messaging;
            this.threadCount = threadCount;

            // Set Address of Binder's callback function for Cabinet Splitting
            this.newCabNamesCallBackAddress = newCabNamesCallBackAddress;
        }

        private IMessaging Messaging { get; }

        public int MaximumCabinetSizeForLargeFileSplitting { get; set; }

        public int MaximumUncompressedMediaSize { get; set; }

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
            // don't create more threads than the number of cabinets to build
            if (this.cabinetWorkItems.Count < this.threadCount)
            {
                this.threadCount = this.cabinetWorkItems.Count;
            }

            if (0 < this.threadCount)
            {
                Thread[] threads = new Thread[this.threadCount];

                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(new ThreadStart(this.ProcessWorkItems));
                    threads[i].Start();
                }

                // wait for all threads to finish
                foreach (Thread thread in threads)
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

                        cabinetWorkItem = (CabinetWorkItem)this.cabinetWorkItems.Dequeue();
                    }

                    // create a cabinet
                    this.CreateCabinet(cabinetWorkItem);
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
        private void CreateCabinet(CabinetWorkItem cabinetWorkItem)
        {
            this.Messaging.Write(VerboseMessages.CreateCabinet(cabinetWorkItem.CabinetFile));

            int maxCabinetSize = 0; // The value of 0 corresponds to default of 2GB which means no cabinet splitting
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

            // create the cabinet file
            var cabinetPath = Path.GetFullPath(cabinetWorkItem.CabinetFile);

            var files = cabinetWorkItem.FileFacades
                .Select(facade => facade.Hash == null ?
                    new CabinetCompressFile(facade.SourcePath, facade.Id + cabinetWorkItem.ModularizationSuffix) :
                    new CabinetCompressFile(facade.SourcePath, facade.Id + cabinetWorkItem.ModularizationSuffix, facade.Hash.HashPart1, facade.Hash.HashPart2, facade.Hash.HashPart3, facade.Hash.HashPart4))
                .ToList();

            var cab = new Cabinet(cabinetPath);
            cab.Compress(files, cabinetWorkItem.CompressionLevel, maxCabinetSize, cabinetWorkItem.MaxThreshold);

            // TODO: Handle newCabNamesCallBackAddress from compression.
        }
    }
}
