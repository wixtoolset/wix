// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Wix Toolset Command-Line Interface.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The main entry point for wix command-line interface.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            var serviceProvider = new WixToolsetServiceProvider();

            var listener = new ConsoleMessageListener("WIX", "wix.exe");

            var program = new Program();
            return program.Run(serviceProvider, listener, args);
        }

        /// <summary>
        /// Executes the wix command-line interface.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="args">Command-line arguments to execute.</param>
        /// <returns>Returns the application error code.</returns>
        public int Run(IServiceProvider serviceProvider, IMessageListener listener, string[] args)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var context = serviceProvider.GetService<ICommandLineContext>();
            context.Messaging = messaging;
            context.ExtensionManager = CreateExtensionManagerWithStandardBackends(serviceProvider);
            context.ParsedArguments = args;

            var commandLine = serviceProvider.GetService<ICommandLine>();
            var command = commandLine.ParseStandardCommandLine(context);
            return command?.Execute() ?? 1;
        }

        private static IExtensionManager CreateExtensionManagerWithStandardBackends(IServiceProvider serviceProvider)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            return extensionManager;
        }

        private class ConsoleMessageListener : IMessageListener
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
}
