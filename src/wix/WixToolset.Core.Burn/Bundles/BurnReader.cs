// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Burn PE reader for the WiX toolset.
    /// </summary>
    /// <remarks>This class encapsulates reading from a stub EXE with containers attached
    /// for dissecting bundled/chained setup packages.</remarks>
    /// <example>
    /// using (BurnReader reader = BurnReader.Open(fileExe, this.core, guid))
    /// {
    ///     reader.ExtractUXContainer(file1, tempFolder);
    /// }
    /// </example>
    internal class BurnReader : BurnCommon
    {
        private bool disposed;

        private BinaryReader binaryReader;
        private readonly Dictionary<string, string> attachedContainerPayloadNames;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Creates a BurnReader for reading a PE file.
        /// </summary>
        /// <param name="messaging">Messaging.</param>
        /// <param name="fileSystem">File system.</param>
        /// <param name="fileExe">File to read.</param>
        private BurnReader(IMessaging messaging, IFileSystem fileSystem, string fileExe)
            : base(messaging, fileExe)
        {
            this.attachedContainerPayloadNames = new Dictionary<string, string>();
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the underlying stream.
        /// </summary>
        public Stream Stream => this.binaryReader?.BaseStream;

        /// <summary>
        /// Opens a Burn reader.
        /// </summary>
        /// <param name="messaging">Messaging.</param>
        /// <param name="fileSystem">File system.</param>
        /// <param name="fileExe">Path to file.</param>
        /// <returns>Burn reader.</returns>
        public static BurnReader Open(IMessaging messaging, IFileSystem fileSystem, string fileExe)
        {
            var binaryReader = new BinaryReader(fileSystem.OpenFile(null, fileExe, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
            var reader = new BurnReader(messaging, fileSystem, fileExe)
            {
                binaryReader = binaryReader,
            };
            reader.Initialize(reader.binaryReader);

            return reader;
        }

        /// <summary>
        /// Gets the UX container from the exe and extracts its contents to the output directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to write extracted files to.</param>
        /// <param name="tempDirectory">Scratch directory.</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ExtractUXContainer(string outputDirectory, string tempDirectory)
        {
            // No UX container to extract
            if (this.AttachedContainers.Count == 0)
            {
                return false;
            }

            if (this.Invalid)
            {
                return false;
            }

            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(outputDirectory);
            var tempCabPath = Path.Combine(tempDirectory, "ux.cab");
            var manifestOriginalPath = Path.Combine(outputDirectory, "0");
            var manifestPath = Path.Combine(outputDirectory, "manifest.xml");
            var uxContainerSlot = this.AttachedContainers[0];

            this.binaryReader.BaseStream.Seek(this.UXAddress, SeekOrigin.Begin);
            using (Stream tempCab = this.fileSystem.OpenFile(null, tempCabPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                BurnCommon.CopyStream(this.binaryReader.BaseStream, tempCab, uxContainerSlot.Size);
            }

            var cabinet = new Cabinet(tempCabPath);
            cabinet.Extract(outputDirectory);

            this.fileSystem.MoveFile(null, manifestOriginalPath, manifestPath);

            var document = new XmlDocument();
            document.Load(manifestPath);
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("burn", document.DocumentElement.NamespaceURI);
            var uxPayloads = document.SelectNodes("/burn:BurnManifest/burn:UX/burn:Payload", namespaceManager);
            var payloads = document.SelectNodes("/burn:BurnManifest/burn:Payload", namespaceManager);

            foreach (XmlNode uxPayload in uxPayloads)
            {
                var sourcePathNode = uxPayload.Attributes.GetNamedItem("SourcePath");
                var filePathNode = uxPayload.Attributes.GetNamedItem("FilePath");

                var sourcePath = Path.Combine(outputDirectory, sourcePathNode.Value);
                var destinationPath = Path.Combine(outputDirectory, filePathNode.Value);

                this.fileSystem.MoveFile(null, sourcePath, destinationPath);
            }

            foreach (XmlNode payload in payloads)
            {
                var packagingNode = payload.Attributes.GetNamedItem("Packaging");

                var packaging = packagingNode.Value;

                if (packaging.Equals("embedded", StringComparison.OrdinalIgnoreCase))
                {
                    var sourcePathNode = payload.Attributes.GetNamedItem("SourcePath");
                    var filePathNode = payload.Attributes.GetNamedItem("FilePath");
                    var containerNode = payload.Attributes.GetNamedItem("Container");

                    var sourcePath = sourcePathNode.Value;
                    var destinationPath = Path.Combine(containerNode.Value, filePathNode.Value);

                    this.attachedContainerPayloadNames[sourcePath] = destinationPath;
                }
            }

            return true;
        }

        /// <summary>
        /// Extracts detached containers to the output directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to write extracted files to.</param>
        /// <param name="tempDirectory">Scratch directory.</param>
        /// <param name="uxOutputDirectory">UX extraction folder. If null or empty, a UX folder will be created within tempDirectory</param>
        /// <param name="containerExtensions">Container extensions</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ExtractDetachedContainers(string outputDirectory, string tempDirectory, string uxOutputDirectory, IEnumerable<IBurnContainerExtension> containerExtensions)
        {
            if (String.IsNullOrEmpty(uxOutputDirectory))
            {
                uxOutputDirectory = Path.Combine(tempDirectory, "UX", "Final");
            }

            var manifestPath = Path.Combine(uxOutputDirectory, "manifest.xml");
            if (!File.Exists(manifestPath))
            {
                var uxTempDirectory = Path.Combine(tempDirectory, "UX", "Temp");
                if (!this.ExtractUXContainer(uxOutputDirectory, uxTempDirectory))
                {
                    return false;
                }
            }

            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(outputDirectory);

            var extensionManifestPath = Path.Combine(uxOutputDirectory, BurnCommon.BundleExtensionDataFileName);
            var document = new XmlDocument();
            document.Load(manifestPath);
            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("burn", BurnCommon.BurnNamespace);
            var detachedContainerNodes = document.SelectNodes("/burn:BurnManifest/burn:Container[not(@Attached = 'yes')]", nsmgr);

            var exeDirectory = Path.GetDirectoryName(this.fileExe);
            foreach (var node in detachedContainerNodes)
            {
                var containerElement = (XmlElement)node;
                var containerName = containerElement.GetAttribute("FilePath");

                var containerId = containerElement.GetAttribute("Id");
                var containerPath = Path.Combine(exeDirectory, containerName);
                var extractDirectory = Path.Combine(tempDirectory, containerName);

                Directory.CreateDirectory(extractDirectory);

                this.ExtractContainer(containerId, containerPath, extractDirectory, containerElement, containerExtensions, extensionManifestPath);

                var containerPayloadNodes = document.SelectNodes($"/burn:BurnManifest/burn:Payload[@Packaging='embedded' and @Container='{containerId}']", nsmgr);
                foreach (var payloadNode in containerPayloadNodes)
                {
                    var payloadElement = (XmlElement)payloadNode;
                    var srcFileName = payloadElement.GetAttribute("SourcePath");
                    var dstFileName = payloadElement.GetAttribute("FilePath");

                    var sourcePath = Path.Combine(extractDirectory, srcFileName);
                    var destinationPath = Path.Combine(outputDirectory, containerName, dstFileName);

                    this.fileSystem.MoveFile(null, sourcePath, destinationPath);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets each non-UX attached container from the exe and extracts its contents to the output directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to write extracted files to.</param>
        /// <param name="tempDirectory">Scratch directory.</param>
        /// <param name="uxOutputDirectory">UX extraction folder. If null or empty, a UX folder will be created within tempDirectory</param>
        /// <param name="containerExtensions">Container extensions</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ExtractAttachedContainers(string outputDirectory, string tempDirectory, string uxOutputDirectory, IEnumerable<IBurnContainerExtension> containerExtensions)
        {
            // No attached containers to extract
            if (this.AttachedContainers.Count < 2)
            {
                return false;
            }

            if (this.Invalid)
            {
                return false;
            }

            if (String.IsNullOrEmpty(uxOutputDirectory))
            {
                uxOutputDirectory = Path.Combine(tempDirectory, "UX", "Final");
            }

            var uxTempDirectory = Path.Combine(tempDirectory, "UX", "Temp");
            if (!this.ExtractUXContainer(uxOutputDirectory, uxTempDirectory))
            {
                return false;
            }

            var extensionManifestPath = Path.Combine(uxOutputDirectory, BurnCommon.BundleExtensionDataFileName);
            var manifestPath = Path.Combine(uxOutputDirectory, "manifest.xml");
            var document = new XmlDocument();
            document.Load(manifestPath);
            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("burn", BurnCommon.BurnNamespace);

            Directory.CreateDirectory(outputDirectory);
            var nextAddress = this.EngineSize;
            for (var i = 1; i < this.AttachedContainers.Count; ++i)
            {
                var cntnr = this.AttachedContainers[i];
                var tempCabPath = Path.Combine(tempDirectory, $"a{i}.data");

                this.binaryReader.BaseStream.Seek(nextAddress, SeekOrigin.Begin);
                using (Stream tempCab = this.fileSystem.OpenFile(null, tempCabPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    BurnCommon.CopyStream(this.binaryReader.BaseStream, tempCab, cntnr.Size);
                }

                if (!(document.SelectSingleNode($"/burn:BurnManifest/burn:Container[@Attached = 'yes' and @AttachedIndex = {i}]", nsmgr) is XmlElement containerElement))
                {
                    this.Messaging.Write(ErrorMessages.InvalidBurnManifestContainers(null, this.AttachedContainers.Count, i));
                    return false;
                }
                var containerId = containerElement.GetAttribute("Id");
                this.ExtractContainer(containerId, tempCabPath, outputDirectory, containerElement, containerExtensions, extensionManifestPath);

                nextAddress += cntnr.Size;
            }

            foreach (var entry in this.attachedContainerPayloadNames)
            {
                var sourcePath = Path.Combine(outputDirectory, (string)entry.Key);
                var destinationPath = Path.Combine(outputDirectory, (string)entry.Value);

                this.fileSystem.MoveFile(null, sourcePath, destinationPath);
            }

            return true;
        }

        private void ExtractContainer(string containerId, string containerPath, string outputDirectory, XmlElement containerElement, IEnumerable<IBurnContainerExtension> containerExtensions, string extensionManifestPath)
        {
            string containerType = containerElement.GetAttribute("Type");
            if (containerType.Equals("Extension"))
            {
                IBurnContainerExtension containerExtension = null;
                string containerExtensionId = containerElement.GetAttribute("ExtensionId");
                if ((containerExtensions != null) && !String.IsNullOrEmpty(containerExtensionId))
                {
                    foreach (var extension in containerExtensions)
                    {
                        if (extension.ContainerExtensionIds.Contains(containerExtensionId))
                        {
                            containerExtension = extension;
                            break;
                        }
                    }
                }

                if (containerExtension == null)
                {
                    this.Messaging.Write(WarningMessages.MissingContainerExtension(null, containerId, containerExtensionId));
                    return;
                }

                // Get extension data from manifest
                var extensionManifestDocument = new XmlDocument();
                extensionManifestDocument.Load(extensionManifestPath);
                var extensionNsmgr = new XmlNamespaceManager(extensionManifestDocument.NameTable);
                extensionNsmgr.AddNamespace("ed", BurnConstants.BundleExtensionDataNamespace);
                var extensionDataNode = extensionManifestDocument.SelectSingleNode($"/ed:BundleExtensionData/ed:BundleExtension[@Id='{containerExtensionId}']", extensionNsmgr) as XmlElement;

                try
                {
                    containerExtension.ExtractContainer(containerPath, outputDirectory, containerId, extensionDataNode);
                }
                catch (Exception ex)
                {
                    this.Messaging.Write(ErrorMessages.ContainerExtractFailed(null, containerId, containerExtensionId, ex.Message));
                    return;
                }
            }
            else
            {
                var cabinet = new Cabinet(containerPath);
                cabinet.Extract(outputDirectory);
            }
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        /// <param name="disposing">True when releasing managed objects.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing && this.binaryReader != null)
                {
                    this.binaryReader.Close();
                    this.binaryReader = null;
                }

                this.disposed = true;
            }
        }
    }
}
