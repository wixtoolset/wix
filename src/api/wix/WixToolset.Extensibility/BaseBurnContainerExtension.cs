// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a Burn container extension.
    /// </summary>
    public abstract class BaseBurnContainerExtension : IBurnContainerExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IBindContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Backend helper for use by the extension.
        /// </summary>
        protected IBurnBackendHelper BackendHelper { get; private set; }

        /// <summary>
        /// Collection of bundle extension IDs that this container extension handles.
        /// </summary>
        public abstract IReadOnlyCollection<string> ContainerExtensionIds { get; }

        /// <summary>
        /// Called at the beginning of the binding phase.
        /// </summary>
        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.BackendHelper = context.ServiceProvider.GetService<IBurnBackendHelper>();
        }

        /// <summary>
        /// Called during bind phase to create a container
        /// Implementors must set <see cref="WixBundleContainerSymbol.Hash"/> to the container file's SHA512, and <see cref="WixBundleContainerSymbol.Size"/> after creating the container
        /// </summary>
        /// <param name="container">The container symbol.</param>
        /// <param name="containerPayloads">Collection of payloads that should be compressed in the container.</param>
        /// <param name="sha512">SHA512 hash of the container file.</param>
        /// <param name="size">File size of the container file.</param>
        public abstract void CreateContainer(WixBundleContainerSymbol container, IEnumerable<WixBundlePayloadSymbol> containerPayloads, out string sha512, out long size);

        /// <summary>
        /// Extract the container to a folder. Called on 'burn extract' command.
        /// Note that, the PreBackendBind may not be called before calling this method.
        /// </summary>
        /// <param name="containerPath"></param>
        /// <param name="outputFolder"></param>
        /// <param name="containerId"></param>
        /// <param name="extensionDataNode"></param>
        public abstract void ExtractContainer(string containerPath, string outputFolder, string containerId, XmlElement extensionDataNode);

        /// <summary>
        /// Helper method to calculate SHA512 and size of the container
        /// </summary>
        /// <param name="containerPath"></param>
        /// <param name="sha512"></param>
        /// <param name="size"></param>
        protected void CalculateHashAndSize(string containerPath, out string sha512, out long size)
        {
            byte[] hashBytes;

            var fileInfo = new FileInfo(containerPath);
            using (var managed = new SHA512CryptoServiceProvider())
            {
                using (var stream = fileInfo.OpenRead())
                {
                    hashBytes = managed.ComputeHash(stream);
                }
            }

            var sb = new StringBuilder(hashBytes.Length * 2);
            for (var i = 0; i < hashBytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", hashBytes[i]);
            }

            sha512 = sb.ToString();
            size = fileInfo.Length;
        }
    }
}
