// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Message listener.
    /// </summary>
    public interface IMessageListener
    {
        /// <summary>
        /// Calculate a new level for a message.
        /// </summary>
        /// <param name="messaging">Messaging object.</param>
        /// <param name="message">Message to evaluate.</param>
        /// <param name="defaultMessageLevel">Current message level.</param>
        /// <returns></returns>
        MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel);

        /// <summary>
        /// Writes a message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        void Write(Message message);

        /// <summary>
        /// Writes a string message.
        /// </summary>
        /// <param name="message">String message to write.</param>
        void Write(string message);
    }
}
