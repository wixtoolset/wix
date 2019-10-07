// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CreateNonUXContainers
    {
        public CreateNonUXContainers(IBackendHelper backendHelper, IntermediateSection section, WixBootstrapperApplicationTuple bootstrapperApplicationTuple, Dictionary<string, WixBundlePayloadTuple> payloadTuples, string intermediateFolder, string layoutFolder, CompressionLevel? defaultCompressionLevel)
        {
            this.BackendHelper = backendHelper;
            this.Section = section;
            this.BootstrapperApplicationTuple = bootstrapperApplicationTuple;
            this.PayloadTuples = payloadTuples;
            this.IntermediateFolder = intermediateFolder;
            this.LayoutFolder = layoutFolder;
            this.DefaultCompressionLevel = defaultCompressionLevel;
        }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        public WixBundleContainerTuple UXContainer { get; set; }

        public IEnumerable<WixBundlePayloadTuple> UXContainerPayloads { get; private set; }

        public IEnumerable<WixBundleContainerTuple> Containers { get; private set; }

        private IBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private WixBootstrapperApplicationTuple BootstrapperApplicationTuple { get; }

        private Dictionary<string, WixBundlePayloadTuple> PayloadTuples { get; }

        private string IntermediateFolder { get; }

        private string LayoutFolder { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public void Execute()
        {
            var fileTransfers = new List<IFileTransfer>();

            var uxPayloadTuples = new List<WixBundlePayloadTuple>();

            var attachedContainerIndex = 1; // count starts at one because UX container is "0".

            var containerTuples = this.Section.Tuples.OfType<WixBundleContainerTuple>().ToList();

            var payloadsByContainer = this.PayloadTuples.Values.ToLookup(p => p.ContainerRef);

            foreach (var container in containerTuples)
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
                    var baPayloadId = this.BootstrapperApplicationTuple.Id.Id;

                    foreach (var uxPayload in containerPayloads)
                    {
                        if (uxPayload.Id.Id == baPayloadId)
                        {
                            uxPayloadTuples.Insert(0, uxPayload);
                        }
                        else
                        {
                            uxPayloadTuples.Add(uxPayload);
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

                    this.CreateContainer(container, containerPayloads, null);
                }
            }

            this.Containers = containerTuples;
            this.UXContainerPayloads = uxPayloadTuples;
            this.FileTransfers = fileTransfers;
        }

        private void CreateContainer(WixBundleContainerTuple container, IEnumerable<WixBundlePayloadTuple> containerPayloads, string manifestFile)
        {
            var command = new CreateContainerCommand(containerPayloads, container.WorkingPath, this.DefaultCompressionLevel);
            command.Execute();

            container.Hash = command.Hash;
            container.Size = command.Size;
        }
    }
}
