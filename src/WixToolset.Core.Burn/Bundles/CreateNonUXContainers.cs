// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CreateNonUXContainers
    {
        public CreateNonUXContainers(IBackendHelper backendHelper, IntermediateSection section, WixBootstrapperApplicationSymbol bootstrapperApplicationSymbol, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols, string intermediateFolder, string layoutFolder, CompressionLevel? defaultCompressionLevel)
        {
            this.BackendHelper = backendHelper;
            this.Section = section;
            this.BootstrapperApplicationSymbol = bootstrapperApplicationSymbol;
            this.PayloadSymbols = payloadSymbols;
            this.IntermediateFolder = intermediateFolder;
            this.LayoutFolder = layoutFolder;
            this.DefaultCompressionLevel = defaultCompressionLevel;
        }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; private set; }

        public WixBundleContainerSymbol UXContainer { get; set; }

        public IEnumerable<WixBundlePayloadSymbol> UXContainerPayloads { get; private set; }

        public IEnumerable<WixBundleContainerSymbol> Containers { get; private set; }

        private IBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private WixBootstrapperApplicationSymbol BootstrapperApplicationSymbol { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PayloadSymbols { get; }

        private string IntermediateFolder { get; }

        private string LayoutFolder { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public void Execute()
        {
            var fileTransfers = new List<IFileTransfer>();
            var trackedFiles = new List<ITrackedFile>();
            var uxPayloadSymbols = new List<WixBundlePayloadSymbol>();

            var attachedContainerIndex = 1; // count starts at one because UX container is "0".

            var containerSymbols = this.Section.Symbols.OfType<WixBundleContainerSymbol>().ToList();

            var payloadsByContainer = this.PayloadSymbols.Values.ToLookup(p => p.ContainerRef);

            foreach (var container in containerSymbols)
            {
                var containerId = container.Id.Id;

                var containerPayloads = payloadsByContainer[containerId];

                if (!containerPayloads.Any())
                {
                    if (containerId != BurnConstants.BurnDefaultAttachedContainerName)
                    {
                        // TODO: display warning that we're ignoring container that ended up with no paylods in it.
                    }
                }
                else if (BurnConstants.BurnUXContainerName == containerId)
                {
                    this.UXContainer = container;

                    container.WorkingPath = Path.Combine(this.IntermediateFolder, container.Name);
                    container.AttachedContainerIndex = 0;

                    // Gather the list of UX payloads but ensure the BootstrapperApplication Payload is the first
                    // in the list since that is the Payload that Burn attempts to load.
                    var baPayloadId = this.BootstrapperApplicationSymbol.Id.Id;

                    foreach (var uxPayload in containerPayloads)
                    {
                        if (uxPayload.Id.Id == baPayloadId)
                        {
                            uxPayloadSymbols.Insert(0, uxPayload);
                        }
                        else
                        {
                            uxPayloadSymbols.Add(uxPayload);
                        }
                    }
                }
                else
                {
                    container.WorkingPath = Path.Combine(this.IntermediateFolder, container.Name);

                    // Add detached containers to the list of file transfers.
                    if (ContainerType.Detached == container.Type)
                    {
                        var transfer = this.BackendHelper.CreateFileTransfer(container.WorkingPath, Path.Combine(this.LayoutFolder, container.Name), true, container.SourceLineNumbers);
                        fileTransfers.Add(transfer);
                    }
                    else // update the attached container index.
                    {
                        Debug.Assert(ContainerType.Attached == container.Type);

                        container.AttachedContainerIndex = attachedContainerIndex;
                        ++attachedContainerIndex;
                    }

                    this.CreateContainer(container, containerPayloads);
                    trackedFiles.Add(this.BackendHelper.TrackFile(container.WorkingPath, TrackedFileType.Temporary, container.SourceLineNumbers));
                }
            }

            this.Containers = containerSymbols;
            this.UXContainerPayloads = uxPayloadSymbols;
            this.FileTransfers = fileTransfers;
            this.TrackedFiles = trackedFiles;
        }

        private void CreateContainer(WixBundleContainerSymbol container, IEnumerable<WixBundlePayloadSymbol> containerPayloads)
        {
            var command = new CreateContainerCommand(containerPayloads, container.WorkingPath, this.DefaultCompressionLevel);
            command.Execute();

            container.Hash = command.Hash;
            container.Size = command.Size;
        }
    }
}
