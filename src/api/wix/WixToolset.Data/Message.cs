// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    /// <summary>
    /// Event args for message events.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Creates a new Message using a format string.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the message.</param>
        /// <param name="level">Message level.</param>
        /// <param name="id">Id for the message.</param>
        /// <param name="format">Format .</param>
        /// <param name="messageArgs">Arguments for the format string.</param>
        public Message(SourceLineNumber sourceLineNumbers, MessageLevel level, int id, string format, params object[] messageArgs)
        {
            this.SourceLineNumbers = sourceLineNumbers;
            this.Level = level;
            this.Id = id;
            this.ResourceNameOrFormat = format;
            this.MessageArgs = messageArgs;
        }

        /// <summary>
        /// Creates a new Message using a format string from a resource manager.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the message.</param>
        /// <param name="level">Message level.</param>
        /// <param name="id">Id for the message.</param>
        /// <param name="resourceManager">Resource manager.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="messageArgs">Arguments for the format string.</param>
        public Message(SourceLineNumber sourceLineNumbers, MessageLevel level, int id, ResourceManager resourceManager, string resourceName, params object[] messageArgs)
        {
            this.SourceLineNumbers = sourceLineNumbers;
            this.Level = level;
            this.Id = id;
            this.ResourceManager = resourceManager;
            this.ResourceNameOrFormat = resourceName;
            this.MessageArgs = messageArgs;
        }

        /// <summary>
        /// Gets the source line numbers.
        /// </summary>
        /// <value>The source line numbers.</value>
        public SourceLineNumber SourceLineNumbers { get; }

        /// <summary>
        /// Gets the Id for the message.
        /// </summary>
        /// <value>The Id for the message.</value>
        public int Id { get; }

        /// <summary>
        /// Gets the resource manager for this event args.
        /// </summary>
        /// <value>The resource manager for this event args.</value>
        public ResourceManager ResourceManager { get; }

        /// <summary>
        /// Gets the name of the resource or format string if no resource manager was provided.
        /// </summary>
        /// <value>The name of the resource or format string.</value>
        public string ResourceNameOrFormat { get; }

        /// <summary>
        /// Gets or sets the <see cref="MessageLevel"/> for the message.
        /// </summary>
        /// <value>The <see cref="MessageLevel"/> for the message.</value>
        public MessageLevel Level { get; private set; }

        /// <summary>
        /// Gets the arguments for the format string.
        /// </summary>
        /// <value>The arguments for the format string.</value>
        public object[] MessageArgs { get; }

        public override string ToString()
        {
            if (this.ResourceManager == null)
            {
                return String.Format(this.ResourceNameOrFormat, this.MessageArgs);
            }
            else
            {
                return String.Format(this.ResourceManager.GetString(this.ResourceNameOrFormat), this.MessageArgs);
            }
        }
    }
}
