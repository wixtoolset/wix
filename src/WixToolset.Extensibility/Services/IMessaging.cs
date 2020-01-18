// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;

    /// <summary>
    /// Interface for handling messages (error/warning/verbose).
    /// </summary>
    public interface IMessaging
    {
        /// <summary>
        /// Indicates whether an error has been found.
        /// </summary>
        /// <value>A bool indicating whether an error has been found.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets the last error code encountered during messaging.
        /// </summary>
        /// <value>The exit code for the process.</value>
        int LastErrorNumber { get; }

        /// <summary>
        /// Gets or sets the option to show verbose messages.
        /// </summary>
        /// <value>The option to show verbose messages.</value>
        bool ShowVerboseMessages { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress all warning messages.
        /// </summary>
        /// <value>The option to suppress all warning messages.</value>
        bool SuppressAllWarnings { get; set; }

        /// <summary>
        /// Gets and sets the option to treat warnings as errors.
        /// </summary>
        /// <value>The option to treat warnings as errors.</value>
        bool WarningsAsError { get; set; }

        /// <summary>
        /// Sets the listener for messaging.
        /// </summary>
        /// <param name="listener"></param>
        void SetListener(IMessageListener listener);

        /// <summary>
        /// Adds a warning message id to be elevated to an error message.
        /// </summary>
        /// <param name="warningNumber">Id of the message to elevate.</param>
        void ElevateWarningMessage(int warningNumber);

        /// <summary>
        /// Adds a warning message id to be suppressed in message output.
        /// </summary>
        /// <param name="warningNumber">Id of the message to suppress.</param>
        void SuppressWarningMessage(int warningNumber);

        /// <summary>
        /// Sends a message with the given arguments.
        /// </summary>
        /// <param name="message">Message to write.</param>
        void Write(Message message);

        /// <summary>
        /// Sends a message with the given arguments.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="verbose">Indicates where to write a verbose message.</param>
        void Write(string message, bool verbose = false);
    }
}
