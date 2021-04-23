// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.Heat
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core;
    using WixToolset.Core.Burn;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters;
    using WixToolset.Tools.Core;

    /// <summary>
    /// Wix Toolset Harvester.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static async Task<int> Main(string[] args)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider()
                                                                  .AddBundleBackend();
            var listener = new ConsoleMessageListener("HEAT", "heat.exe");

            try
            {
                var program = new Program();
                return await program.Run(serviceProvider, listener, args);
            }
            catch (WixException e)
            {
                listener.Write(e.Error);

                return e.Error.Id;
            }
            catch (Exception e)
            {
                listener.Write(ErrorMessages.UnexpectedException(e));

                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }

                return e.HResult;
            }
        }

        /// <summary>
        /// Run the application with the given arguments.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>Returns the application error code.</returns>
        public Task<int> Run(IServiceProvider serviceProvider, IMessageListener listener, string[] args)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            var commandLine = HeatCommandLineFactory.CreateCommandLine(serviceProvider);
            var command = commandLine.ParseStandardCommandLine(arguments);
            return command?.ExecuteAsync(CancellationToken.None) ?? Task.FromResult(1);
        }
    }
}
