// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ProcessPayloadsCommand
    {
        public ProcessPayloadsCommand(IWixToolsetServiceProvider serviceProvider, IBackendHelper backendHelper, IPayloadHarvester payloadHarvester, IEnumerable<WixBundlePayloadSymbol> payloads, PackagingType defaultPackaging, string layoutDirectory)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();

            this.BackendHelper = backendHelper;
            this.PayloadHarvester = payloadHarvester;
            this.Payloads = payloads;
            this.DefaultPackaging = defaultPackaging;
            this.LayoutDirectory = layoutDirectory;
        }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; private set; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IPayloadHarvester PayloadHarvester { get; }

        private IEnumerable<WixBundlePayloadSymbol> Payloads { get; }

        private PackagingType DefaultPackaging { get; }

        private string LayoutDirectory { get; }

        public void Execute()
        {
            var fileTransfers = new List<IFileTransfer>();
            var trackedFiles = new List<ITrackedFile>();

            foreach (var payload in this.Payloads)
            {
                payload.Name = this.BackendHelper.GetCanonicalRelativePath(payload.SourceLineNumbers, "Payload", "Name", payload.Name);

                // Embedded files (aka: files from binary .wixlibs) are not content files (because they are hidden
                // in the .wixlib).
                var sourceFile = payload.SourceFile;
                payload.ContentFile = sourceFile != null && !sourceFile.Embed;

                this.UpdatePayloadPackagingType(payload);

                if (!this.PayloadHarvester.HarvestStandardInformation(payload))
                {
                    // Remote payloads obviously cannot be embedded.
                    Debug.Assert(PackagingType.Embedded != payload.Packaging);
                }
                else // not a remote payload so we have a lot more to update.
                {
                    // External payloads need to be transfered.
                    if (PackagingType.External == payload.Packaging)
                    {
                        var transfer = this.BackendHelper.CreateFileTransfer(sourceFile.Path, Path.Combine(this.LayoutDirectory, payload.Name), false, payload.SourceLineNumbers);
                        fileTransfers.Add(transfer);
                    }

                    if (payload.ContentFile)
                    {
                        trackedFiles.Add(this.BackendHelper.TrackFile(sourceFile.Path, TrackedFileType.Input, payload.SourceLineNumbers));
                    }
                }
            }

            this.FileTransfers = fileTransfers;
            this.TrackedFiles = trackedFiles;
        }

        private void UpdatePayloadPackagingType(WixBundlePayloadSymbol payload)
        {
            if (!payload.Packaging.HasValue || PackagingType.Unknown == payload.Packaging)
            {
                if (!payload.Compressed.HasValue)
                {
                    payload.Packaging = this.DefaultPackaging;
                }
                else if (payload.Compressed.Value)
                {
                    payload.Packaging = PackagingType.Embedded;
                }
                else
                {
                    payload.Packaging = PackagingType.External;
                }
            }

            // Embedded payloads that are not assigned a container already are placed in the default attached
            // container.
            if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.ContainerRef))
            {
                payload.ContainerRef = BurnConstants.BurnDefaultAttachedContainerName;
            }
        }
    }
}
