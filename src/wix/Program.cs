// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Data;
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
            Messaging.Instance.InitializeAppName("WIX", "wix.exe");
            Messaging.Instance.Display += DisplayMessage;

            var serviceProvider = new WixToolsetServiceProvider();
            var program = new Program();
            return program.Run(serviceProvider, args);
        }

        /// <summary>
        /// Executes the wix command-line interface.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="args">Command-line arguments to execute.</param>
        /// <returns>Returns the application error code.</returns>
        public int Run(IServiceProvider serviceProvider, string[] args)
        {
            var context = serviceProvider.GetService<ICommandLineContext>();
            context.Messaging = Messaging.Instance;
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

        private static void DisplayMessage(object sender, DisplayEventArgs e)
        {
            switch (e.Level)
            {
                case MessageLevel.Warning:
                case MessageLevel.Error:
                    Console.Error.WriteLine(e.Message);
                    break;
                default:
                    Console.WriteLine(e.Message);
                    break;
            }
        }
    }
}
