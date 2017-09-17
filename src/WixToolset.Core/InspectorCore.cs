// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Core facilities for inspector extensions.
    /// </summary>
    internal sealed class InspectorCore : IInspectorCore
    {
        /// <summary>
        /// Gets whether an error occured.
        /// </summary>
        /// <value>Whether an error occured.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Logs a message to the log handler.
        /// </summary>
        /// <param name="e">The <see cref="MessageEventArgs"/> that contains information to log.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
