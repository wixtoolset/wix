// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using WixToolset.Data;

    /// <summary>
    /// Melts a Module Wix document into a ComponentGroup representation.
    /// </summary>
    public sealed class MelterCore : IMessageHandler
    {
        /// <summary>
        /// Gets whether the melter core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
