// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.WixCop
{
    using System;
    using WixToolset.Core;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.Core;
    using WixToolset.Tools.WixCop.CommandLine;
    using WixToolset.Tools.WixCop.Interfaces;

    /// <summary>
    /// Wix source code style inspector and converter.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var listener = new ConsoleMessageListener("WXCP", "wixcop.exe");

            serviceProvider.AddService<IMessageListener>((x, y) => listener);
            serviceProvider.AddService<IWixCopCommandLineParser>((x, y) => new WixCopCommandLineParser(x));

            var program = new Program();
            return program.Run(serviceProvider, args);
        }

        /// <summary>
        /// Run the application with the given arguments.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        public int Run(IWixToolsetServiceProvider serviceProvider, string[] args)
        {
            try
            {
                var listener = serviceProvider.GetService<IMessageListener>();
                var messaging = serviceProvider.GetService<IMessaging>();
                messaging.SetListener(listener);

                var arguments = serviceProvider.GetService<ICommandLineArguments>();
                arguments.Populate(args);

                var commandLine = serviceProvider.GetService<IWixCopCommandLineParser>();
                commandLine.Arguments = arguments;
                var command = commandLine.ParseWixCopCommandLine();
                return command?.Execute() ?? 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("wixcop.exe : fatal error WXCP0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

                return 1;
            }
        }
    }
}
