// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public sealed class ConsoleMessageListener : IMessageListener
    {
        public ConsoleMessageListener(string prefix, string appName)
        {
            this.Prefix = prefix;
            this.AppName = appName;

            PrepareConsoleForLocalization();
        }

        public string AppName { get; }

        public string Prefix { get; }

        public void Write(Message message)
        {
            var filename = message.SourceLineNumbers?.FileName ?? this.AppName;
            var type = message.Level.ToString().ToLowerInvariant();
            var output = message.Level >= MessageLevel.Warning ? Console.Out : Console.Error;

            if (message.SourceLineNumbers?.LineNumber.HasValue == true)
            {
                filename = String.Concat(filename, "(", message.SourceLineNumbers?.LineNumber.Value, ")");
            }

            output.WriteLine("{0} : {1} {2}{3:0000}: {4}", filename, type, this.Prefix, message.Id, message.ToString());

            var fileNames = GetFileNames(message.SourceLineNumbers);
            if (fileNames.Count > 1)
            {
                foreach (var fileName in fileNames)
                {
                    output.WriteLine("Source trace: {0}", fileName);
                }
            }
        }

        public void Write(string message) => Console.Out.WriteLine(message);

        public MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel) => defaultMessageLevel;

        private static IList<string> GetFileNames(SourceLineNumber sourceLineNumbers)
        {
            var fileNames = new List<string>();

            for (var sln = sourceLineNumbers; null != sln; sln = sln.Parent)
            {
                if (String.IsNullOrEmpty(sln.FileName))
                {
                    continue;
                }
                else if (sln.LineNumber.HasValue)
                {
                    fileNames.Add(String.Format(CultureInfo.CurrentUICulture, "{0}: line {1}", sln.FileName, sln.LineNumber));
                }
                else
                {
                    fileNames.Add(sln.FileName);
                }
            }

            return fileNames;
        }

        private static void PrepareConsoleForLocalization()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture.GetConsoleFallbackUICulture();

            if (Console.OutputEncoding.CodePage != Encoding.UTF8.CodePage &&
                Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.OEMCodePage &&
                Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.ANSICodePage)
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }
        }
    }
}
