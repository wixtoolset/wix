// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Converters;
    using WixToolset.Core;
    using WixToolset.Core.Burn;
    using WixToolset.Core.ExtensionCache;
    using WixToolset.Core.WindowsInstaller;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.Core;

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
        public static async Task<int> Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var listener = new ConsoleMessageListener("WIX", "wix.exe");

            Console.CancelKeyPress += (s, e) =>
            {
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider()
                                                                      .AddWindowsInstallerBackend()
                                                                      .AddBundleBackend()
                                                                      .AddExtensionCacheManager()
                                                                      .AddConverter();

                return await Run(serviceProvider, listener, args, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return -1;
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
        /// Executes the wix command-line interface.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="listener">Listener to use for the messaging system.</param>
        /// <param name="args">Command-line arguments to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Returns the application error code.</returns>
        public static Task<int> Run(IServiceProvider serviceProvider, IMessageListener listener, string[] args, CancellationToken cancellationToken)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var commandLine = serviceProvider.GetService<ICommandLine>();
            var command = commandLine.CreateCommand(args);
            return command?.ExecuteAsync(cancellationToken) ?? Task.FromResult(1);
        }
    }
}
