using System;
using System.Globalization;
using System.Text;
using System.Threading;
using WixToolset.Data;
using WixToolset.Extensibility;

namespace WixToolset.Tools.Core
{
    public sealed class ConsoleMessageListener : IMessageListener
    {
        public ConsoleMessageListener(string shortName, string longName)
        {
            this.ShortAppName = shortName;
            this.LongAppName = longName;

            PrepareConsoleForLocalization();
        }

        public string LongAppName { get; }

        public string ShortAppName { get; }

        public void Write(Message message)
        {
            var filename = message.SourceLineNumbers?.FileName ?? this.LongAppName;
            var line = message.SourceLineNumbers?.LineNumber ?? -1;
            var type = message.Level.ToString().ToLowerInvariant();
            var output = message.Level >= MessageLevel.Warning ? Console.Out : Console.Error;

            if (line > 0)
            {
                filename = String.Concat(filename, "(", line, ")");
            }

            output.WriteLine("{0} : {1} {2}{3:0000}: {4}", filename, type, this.ShortAppName, message.Id, message.ToString());
        }

        public void Write(string message)
        {
            Console.Out.WriteLine(message);
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
