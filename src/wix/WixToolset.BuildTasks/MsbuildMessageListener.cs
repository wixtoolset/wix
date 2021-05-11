// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public sealed class MsbuildMessageListener : IMessageListener
    {
        public MsbuildMessageListener(TaskLoggingHelper logger, string shortName, string longName)
        {
            this.Logger = logger;
            this.ShortAppName = shortName;
            this.LongAppName = longName;
        }

        public string ShortAppName { get; }

        public string LongAppName { get; }

        private TaskLoggingHelper Logger { get; }

        public void Write(Message message)
        {
            var code = this.ShortAppName + message.Id.ToString();
            var file = message.SourceLineNumbers?.FileName ?? this.LongAppName;
            var lineNumber = message.SourceLineNumbers?.LineNumber ?? 0;
            switch (message.Level)
            {
                case MessageLevel.Error:
                    this.Logger.LogError(null, code, null, file, lineNumber, 0, 0, 0, message.ResourceNameOrFormat, message.MessageArgs);
                    break;

                case MessageLevel.Verbose:
                    this.Logger.LogMessage(null, code, null, file, lineNumber, 0, 0, 0, MessageImportance.Low, message.ResourceNameOrFormat, message.MessageArgs);
                    break;

                case MessageLevel.Warning:
                    this.Logger.LogWarning(null, code, null, file, lineNumber, 0, 0, 0, message.ResourceNameOrFormat, message.MessageArgs);
                    break;

                default:
                    if (message.Id > 0)
                    {
                        this.Logger.LogMessage(null, code, null, file, lineNumber, 0, 0, 0, MessageImportance.Normal, message.ResourceNameOrFormat, message.MessageArgs);
                    }
                    else
                    {
                        this.Logger.LogMessage(MessageImportance.Normal, message.ResourceNameOrFormat, message.MessageArgs);
                    }
                    break;
            }
        }

        public void Write(string message)
        {
            this.Logger.LogMessage(MessageImportance.Low, message);
        }

        public MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel) => defaultMessageLevel;
    }
}
#endif
