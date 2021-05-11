// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class Messaging : IMessaging
    {
        private IMessageListener listener;
        private readonly HashSet<int> suppressedWarnings = new HashSet<int>();
        private readonly HashSet<int> warningsAsErrors = new HashSet<int>();

        public bool EncounteredError { get; private set; }

        public int LastErrorNumber { get; private set; }

        public bool ShowVerboseMessages { get; set; }

        public bool SuppressAllWarnings { get; set; }

        public bool WarningsAsError { get; set; }

        public void ElevateWarningMessage(int warningNumber) => this.warningsAsErrors.Add(warningNumber);

        public void SetListener(IMessageListener listener) => this.listener = listener;

        public void SuppressWarningMessage(int warningNumber) => this.suppressedWarnings.Add(warningNumber);

        public void Write(Message message)
        {
            var level = this.CalculateMessageLevel(message);

            if (level == MessageLevel.Nothing)
            {
                return;
            }

            if (level == MessageLevel.Error)
            {
                this.EncounteredError = true;
                this.LastErrorNumber = message.Id;
            }

            if (this.listener != null)
            {
                this.listener.Write(message);
            }
            else if (level == MessageLevel.Error)
            {
                throw new WixException(message);
            }
        }

        public void Write(string message, bool verbose = false)
        {
            if (!verbose || this.ShowVerboseMessages)
            {
                this.listener?.Write(message);
            }
        }

        /// <summary>
        /// Determines the level of this message, when taking into account warning-as-error, 
        /// warning level, verbosity level and message suppressed by the caller.
        /// </summary>
        /// <param name="message">Event arguments for the message.</param>
        /// <returns>MessageLevel representing the level of this message.</returns>
        private MessageLevel CalculateMessageLevel(Message message)
        {
            var level = message.Level;

            if (level == MessageLevel.Verbose)
            {
                if (!this.ShowVerboseMessages)
                {
                    level = MessageLevel.Nothing;
                }
            }
            else if (level == MessageLevel.Warning)
            {
                if (this.SuppressAllWarnings || this.suppressedWarnings.Contains(message.Id))
                {
                    level = MessageLevel.Nothing;
                }
                else if (this.WarningsAsError || this.warningsAsErrors.Contains(message.Id))
                {
                    level = MessageLevel.Error;
                }
            }

            level = this.listener?.CalculateMessageLevel(this, message, level) ?? level;

            return level;
        }
    }
}
