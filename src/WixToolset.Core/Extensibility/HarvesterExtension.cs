// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The base harvester extension.  Any of these methods can be overridden to change
    /// the behavior of the harvester.
    /// </summary>
    public abstract class HarvesterExtension
    {
        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        public IHarvesterCore Core { get; set; }

        /// <summary>
        /// Harvest a WiX document.
        /// </summary>
        /// <param name="argument">The argument for harvesting.</param>
        /// <returns>The harvested Fragments.</returns>
        public abstract Wix.Fragment[] Harvest(string argument);
    }
}
