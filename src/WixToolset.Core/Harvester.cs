// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using WixToolset.Data;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The WiX Toolset harvester.
    /// </summary>
    public sealed class Harvester
    {
        private HarvesterExtension harvesterExtension;

        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        public IHarvesterCore Core { get; set; }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>The extension.</value>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public HarvesterExtension Extension
        {
            get
            {
                return this.harvesterExtension;
            }
            set
            {
                if (null != this.harvesterExtension)
                {
                    throw new InvalidOperationException(WixStrings.EXP_MultipleHarvesterExtensionsSpecified);
                }

                this.harvesterExtension = value;
            }
        }

        /// <summary>
        /// Harvest wix authoring.
        /// </summary>
        /// <param name="argument">The argument for harvesting.</param>
        /// <returns>The harvested wix authoring.</returns>
        public Wix.Wix Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            if (null == this.harvesterExtension)
            {
                throw new WixException(WixErrors.HarvestTypeNotFound());
            }

            this.harvesterExtension.Core = this.Core;

            Wix.Fragment[] fragments = this.harvesterExtension.Harvest(argument);
            if (null == fragments || 0 == fragments.Length)
            {
                return null;
            }

            Wix.Wix wix = new Wix.Wix();
            foreach (Wix.Fragment fragment in fragments)
            {
                wix.AddChild(fragment);
            }

            return wix;
        }
    }
}
