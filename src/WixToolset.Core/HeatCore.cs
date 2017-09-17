// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibilty;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public sealed class HeatCore : IHeatCore, IMessageHandler
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
        /// Gets whether the mutator core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
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

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            Messaging.Instance.OnMessage(mea);
        }
    }
}
