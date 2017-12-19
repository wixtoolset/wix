// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class Messaging : IMessaging
    {
        private IMessageListener listener;
        private HashSet<int> suppressedWarnings = new HashSet<int>();
        private HashSet<int> warningsAsErrors = new HashSet<int>();

        public bool EncounteredError { get; private set; }

        public int LastErrorNumber { get; private set; }

        public bool ShowVerboseMessages { get; set; }

        public bool SuppressAllWarnings { get; set; }

        public bool WarningsAsError { get; set; }

        public void ElevateWarningMessage(int warningNumber)
        {
            this.warningsAsErrors.Add(warningNumber);
        }

        public void SetListener(IMessageListener listener)
        {
            this.listener = listener;
        }

        public void SuppressWarningMessage(int warningNumber)
        {
            this.suppressedWarnings.Add(warningNumber);
        }

        public string FormatMessage(Message message)
        {
            var level = CalculateMessageLevel(message);

            if (level == MessageLevel.Nothing)
            {
                return String.Empty;
            }

            var shortAppName = String.IsNullOrEmpty(this.listener?.ShortAppName) ? "WIX" : this.listener.ShortAppName;
            var longAppName = String.IsNullOrEmpty(this.listener?.LongAppName) ? "WIX" : this.listener.LongAppName;

            var fileNames = new List<string>();
            var errorFileName = longAppName;
            for (var sln = message.SourceLineNumbers; null != sln; sln = sln.Parent)
            {
                if (String.IsNullOrEmpty(sln.FileName))
                {
                    continue;
                }
                else if (sln.LineNumber.HasValue)
                {
                    if (fileNames.Count == 0)
                    {
                        errorFileName = String.Format(CultureInfo.CurrentUICulture, WixStrings.Format_FirstLineNumber, sln.FileName, sln.LineNumber);
                    }

                    fileNames.Add(String.Format(CultureInfo.CurrentUICulture, WixStrings.Format_LineNumber, sln.FileName, sln.LineNumber));
                }
                else
                {
                    if (fileNames.Count == 0)
                    {
                        errorFileName = sln.FileName;
                    }

                    fileNames.Add(sln.FileName);
                }
            }

            var levelString = String.Empty;
            if (MessageLevel.Warning == level)
            {
                levelString = WixStrings.MessageType_Warning;
            }
            else if (MessageLevel.Error == level)
            {
                levelString = WixStrings.MessageType_Error;
            }

            string formatted;
            if (message.ResourceManager == null)
            {
                formatted = String.Format(CultureInfo.InvariantCulture, message.ResourceNameOrFormat, message.MessageArgs);
            }
            else
            {
                formatted = String.Format(CultureInfo.InvariantCulture, message.ResourceManager.GetString(message.ResourceNameOrFormat), message.MessageArgs);
            }

            var builder = new StringBuilder();
            if (level == MessageLevel.Information || level == MessageLevel.Verbose)
            {
                builder.AppendFormat(WixStrings.Format_InfoMessage, formatted);
            }
            else
            {
                builder.AppendFormat(WixStrings.Format_NonInfoMessage, errorFileName, levelString, shortAppName, message.Id, formatted);
            }

            if (fileNames.Count > 1)
            {
                builder.AppendFormat(WixStrings.INF_SourceTrace, Environment.NewLine);

                foreach (var fileName in fileNames)
                {
                    builder.AppendFormat(WixStrings.INF_SourceTraceLocation, fileName, Environment.NewLine);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public void Write(Message message)
        {
            var level = CalculateMessageLevel(message);

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

            return level;
        }
    }
}
