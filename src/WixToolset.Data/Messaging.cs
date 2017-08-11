// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;

    public class Messaging : IMessageHandler
    {
        private static readonly Messaging instance = new Messaging();

        private HashSet<int> suppressedWarnings = new HashSet<int>();
        private HashSet<int> warningsAsErrors = new HashSet<int>();
        private string longAppName;
        private string shortAppName;

        static Messaging()
        {
        }

        private Messaging()
        {
        }

        public static Messaging Instance { get { return Messaging.instance; } }

        /// <summary>
        /// Event fired when messages are to be displayed.
        /// </summary>
        public event DisplayEventHandler Display;

        /// <summary>
        /// Gets a bool indicating whether an error has been found.
        /// </summary>
        /// <value>A bool indicating whether an error has been found.</value>
        public bool EncounteredError { get; private set; }

        /// <summary>
        /// Gets the last error code encountered during messaging.
        /// </summary>
        /// <value>The exit code for the process.</value>
        public int LastErrorNumber { get; private set; }

        /// <summary>
        /// Gets or sets the option to show verbose messages.
        /// </summary>
        /// <value>The option to show verbose messages.</value>
        public bool ShowVerboseMessages { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress all warning messages.
        /// </summary>
        /// <value>The option to suppress all warning messages.</value>
        public bool SuppressAllWarnings { get; set; }

        /// <summary>
        /// Gets and sets the option to treat warnings as errors.
        /// </summary>
        /// <value>The option to treat warnings as errors.</value>
        public bool WarningsAsError { get; set; }

        /// <summary>
        /// Implements IMessageHandler to display error messages.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            MessageLevel messageLevel = this.CalculateMessageLevel(mea);

            if (MessageLevel.Nothing == messageLevel)
            {
                return;
            }
            else if (MessageLevel.Error == messageLevel)
            {
                this.EncounteredError = true;
                this.LastErrorNumber = mea.Id;
            }

            if (null != this.Display)
            {
                string message = mea.GenerateMessageString(this.shortAppName, this.longAppName, messageLevel);
                if (!String.IsNullOrEmpty(message))
                {
                    this.Display(this, new DisplayEventArgs() { Level = messageLevel, Message = message });
                }
            }
            else if (MessageLevel.Error == mea.Level)
            {
                throw new WixException(mea);
            }
        }

        /// <summary>
        /// Sets the app names.
        /// </summary>
        /// <param name="shortName">Short application name; usually 4 uppercase characters.</param>
        /// <param name="longName">Long application name; usually the executable name.</param>
        public Messaging InitializeAppName(string shortName, string longName)
        {
            this.EncounteredError = false;
            this.LastErrorNumber = 0;

            this.Display = null;
            this.ShowVerboseMessages = false;
            this.SuppressAllWarnings = false;
            this.WarningsAsError = false;
            this.suppressedWarnings.Clear();
            this.warningsAsErrors.Clear();

            this.shortAppName = shortName;
            this.longAppName = longName;

            return this;
        }

        /// <summary>
        /// Adds a warning message id to be elevated to an error message.
        /// </summary>
        /// <param name="warningNumber">Id of the message to elevate.</param>
        /// <remarks>
        /// Suppressed warnings will not be elevated as errors.
        /// </remarks>
        public void ElevateWarningMessage(int warningNumber)
        {
            this.warningsAsErrors.Add(warningNumber);
        }

        /// <summary>
        /// Adds a warning message id to be suppressed in message output.
        /// </summary>
        /// <param name="warningNumber">Id of the message to suppress.</param>
        /// <remarks>
        /// Suppressed warnings will not be elevated as errors.
        /// </remarks>
        public void SuppressWarningMessage(int warningNumber)
        {
            this.suppressedWarnings.Add(warningNumber);
        }

        /// <summary>
        /// Determines the level of this message, when taking into account warning-as-error, 
        /// warning level, verbosity level and message suppressed by the caller.
        /// </summary>
        /// <param name="mea">Event arguments for the message.</param>
        /// <returns>MessageLevel representing the level of this message.</returns>
        private MessageLevel CalculateMessageLevel(MessageEventArgs mea)
        {
            MessageLevel messageLevel = mea.Level;

            if (MessageLevel.Verbose == messageLevel)
            {
                if (!this.ShowVerboseMessages)
                {
                    messageLevel = MessageLevel.Nothing;
                }
            }
            else if (MessageLevel.Warning == messageLevel)
            {
                if (this.SuppressAllWarnings || this.suppressedWarnings.Contains(mea.Id))
                {
                    messageLevel = MessageLevel.Nothing;
                }
                else if (this.WarningsAsError || this.warningsAsErrors.Contains(mea.Id))
                {
                    messageLevel = MessageLevel.Error;
                }
            }

            return messageLevel;
        }
    }
}
