// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Xml;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface for container extensions.
    /// </summary>
    public interface IBurnContainerExtension
    {
        /// <summary>
        /// Collection of bundle extension IDs that this container extension handles.
        /// </summary>
        IReadOnlyCollection<string> ContainerExtensionIds { get; }

        /// <summary>
        /// Called at the beginning of the binding phase.
        /// </summary>
        void PreBackendBind(IBindContext context);

        /// <summary>
        /// Called during bind phase to create a container
        /// Implementors must set <see cref="WixBundleContainerSymbol.Hash"/> to the container file's SHA512, and <see cref="WixBundleContainerSymbol.Size"/> after creating the container
        /// </summary>
        /// <param name="container">The container symbol.</param>
        /// <param name="containerPayloads">Collection of payloads that should be compressed in the container.</param>
        /// <param name="sha512">SHA512 hash of the container file.</param>
        /// <param name="size">File size of the container file.</param>
        void CreateContainer(WixBundleContainerSymbol container, IEnumerable<WixBundlePayloadSymbol> containerPayloads, out string sha512, out long size);

        /// <summary>
        /// Extract the container to a folder. Called on 'burn extract' command.
        /// Note that, the PreBackendBind may not be called when calling this method.
        /// </summary>
        /// <param name="containerPath"></param>
        /// <param name="outputFolder"></param>
        /// <param name="containerId"></param>
        /// <param name="extensionDataNode"></param>
        void ExtractContainer(string containerPath, string outputFolder, string containerId, XmlElement extensionDataNode);
    }
}
