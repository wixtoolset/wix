// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using WixToolset.Data;
    using WixToolset.Harvesters.Extensibility;
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// The WiX Toolset harvester.
    /// </summary>
    internal class Harvester : IHarvester
    {
        private IHarvesterExtension harvesterExtension;

        public IHarvesterCore Core { get; set; }

        public IHarvesterExtension Extension
        {
            get
            {
                return this.harvesterExtension;
            }
            set
            {
                if (null != this.harvesterExtension)
                {
                    throw new InvalidOperationException("Multiple harvester extensions specified.");
                }

                this.harvesterExtension = value;
            }
        }

        public Wix.Wix Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            if (null == this.harvesterExtension)
            {
                throw new WixException(ErrorMessages.HarvestTypeNotFound());
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
