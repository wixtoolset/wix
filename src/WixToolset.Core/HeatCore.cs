// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Core.Extensibility;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public class HeatCore : IHeatCore
    {
        private Harvester harvester;
        private Mutator mutator;

        /// <summary>
        /// Instantiates a new HeatCore.
        /// </summary>
        /// <param name="messageHandler">The message handler for the core.</param>
        public HeatCore()
        {
            this.harvester = new Harvester();
            this.mutator = new Mutator();
        }

        /// <summary>
        /// Gets the harvester.
        /// </summary>
        /// <value>The harvester.</value>
        public Harvester Harvester
        {
            get { return this.harvester; }
        }

        /// <summary>
        /// Gets the mutator.
        /// </summary>
        /// <value>The mutator.</value>
        public Mutator Mutator
        {
            get { return this.mutator; }
        }
    }
}
