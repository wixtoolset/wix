// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Extensibility
{
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// Interface for the harvester.
    /// </summary>
    public interface IHarvester
    {
        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        IHarvesterCore Core { get; }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>The extension.</value>
        IHarvesterExtension Extension { get; set; }

        /// <summary>
        /// Harvest wix authoring.
        /// </summary>
        /// <param name="argument">The argument for harvesting.</param>
        /// <returns>The harvested wix authoring.</returns>
        Wix.Wix Harvest(string argument);
    }
}
