// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Extensibility
{
    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public interface IHeatCore
    {
        /// <summary>
        /// Gets the harvester.
        /// </summary>
        /// <value>The harvester.</value>
        Harvester Harvester { get; }

        /// <summary>
        /// Gets the mutator.
        /// </summary>
        /// <value>The mutator.</value>
        Mutator Mutator { get; }
    }
}
