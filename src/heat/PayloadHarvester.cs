// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.IO;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a payload from the file system.
    /// </summary>
    internal class PayloadHarvester : BaseHarvesterExtension
    {
        private bool setUniqueIdentifiers;
        private WixBundlePackageType packageType;

        private IPayloadHarvester payloadHarvester;

        /// <summary>
        /// Instantiate a new PayloadHarvester.
        /// </summary>
        public PayloadHarvester(IPayloadHarvester payloadHarvester, WixBundlePackageType packageType)
        {
            this.payloadHarvester = payloadHarvester;

            this.packageType = packageType;
            this.setUniqueIdentifiers = true;
        }

        /// <summary>
        /// Gets of sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Harvest a payload.
        /// </summary>
        /// <param name="argument">The path of the payload.</param>
        /// <returns>A harvested payload.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            string fullPath = Path.GetFullPath(argument);

            var remotePayload = this.HarvestRemotePayload(fullPath);

            var fragment = new Wix.Fragment();
            fragment.AddChild(remotePayload);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a payload.
        /// </summary>
        /// <param name="path">The path of the payload.</param>
        /// <returns>A harvested payload.</returns>
        public Wix.RemotePayload HarvestRemotePayload(string path)
        {
            if (null == path)
            {
                throw new ArgumentNullException("path");
            }

            if (!File.Exists(path))
            {
                throw new WixException(HarvesterErrors.FileNotFound(path));
            }

            Wix.RemotePayload remotePayload;

            switch (this.packageType)
            {
                case WixBundlePackageType.Exe:
                    remotePayload = new Wix.ExePackagePayload();
                    break;
                case WixBundlePackageType.Msu:
                    remotePayload = new Wix.MsuPackagePayload();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var payloadSymbol = new WixBundlePayloadSymbol
            {
                SourceFile = new IntermediateFieldPathValue { Path = path },
            };

            this.payloadHarvester.HarvestStandardInformation(payloadSymbol);

            if (payloadSymbol.FileSize.HasValue)
            {
                remotePayload.Size = payloadSymbol.FileSize.Value;
            }
            remotePayload.Hash = payloadSymbol.Hash;

            if (!String.IsNullOrEmpty(payloadSymbol.Version))
            {
                remotePayload.Version = payloadSymbol.Version;
            }

            if (!String.IsNullOrEmpty(payloadSymbol.Description))
            {
                remotePayload.Description = payloadSymbol.Description;
            }

            if (!String.IsNullOrEmpty(payloadSymbol.DisplayName))
            {
                remotePayload.ProductName = payloadSymbol.DisplayName;
            }

            return remotePayload;
        }
    }
}
