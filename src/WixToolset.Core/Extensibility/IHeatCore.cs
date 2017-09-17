// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibilty
{
    using WixToolset.Data;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public interface IHeatCore
    {
        /// <summary>
        /// Gets whether the mutator core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

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

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        void OnMessage(MessageEventArgs mea);
    }
}
