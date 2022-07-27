// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Extensibility
{
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public interface IHeatCore
    {
        /// <summary>
        /// Gets the harvester.
        /// </summary>
        /// <value>The harvester.</value>
        IHarvester Harvester { get; }

        /// <summary>
        /// 
        /// </summary>
        IMessaging Messaging { get; }

        /// <summary>
        /// Gets the mutator.
        /// </summary>
        /// <value>The mutator.</value>
        IMutator Mutator { get; }
    }
}
