// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    /// <summary>
    /// Base class for all WiX exceptions.
    /// </summary>
    [Serializable]
    public class WixException : Exception
    {
        /// <summary>
        /// Instantiate a new WixException.
        /// </summary>
        public WixException()
        {
        }

        /// <summary>
        /// Instantiate a new WixException with a simple string message.
        /// </summary>
        /// <param name="message">Simple string message.</param>
        public WixException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiate a new WixException with a simple message and exception.
        /// </summary>
        /// <param name="message">Simple string message.</param>
        /// <param name="innerException">Inner exception.</param>
        public WixException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Instantiate a new WixException with a given Message.
        /// </summary>
        /// <param name="error">The localized error information.</param>
        public WixException(Message error)
            : this(error, null)
        {
        }

        /// <summary>
        /// Instantiate a new WixException with a given Message.
        /// </summary>
        /// <param name="error">The error information as a Message.</param>
        /// <param name="exception">Original exception.</param>
        public WixException(Message error, Exception exception)
            : base(error.ToString(), exception)
        {
            this.Error = error;
        }

        /// <summary>
        /// Instantiate a new WixException with a given source and message.
        /// </summary>
        /// <param name="error">The error information as a Message.</param>
        /// <param name="exception">Original exception.</param>
        /// <param name="detailed">Flag to include the source line numbers and inner exception details.</param>
        public WixException(Message error, Exception exception, bool detailed)
            : base($"{(error.SourceLineNumbers is null ? String.Empty : String.Concat(error.SourceLineNumbers.ToString(), " : "))}{error}{(exception is null ? String.Empty : String.Concat(" : ", exception.ToString()))}", exception)
        {
            this.Error = error;

            this.Source = error.SourceLineNumbers is null ? String.Empty : error.SourceLineNumbers.ToString();
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public Message Error { get; }
    }
}
