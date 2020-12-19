// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Burn;
    using WixToolset.Core.WindowsInstaller;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Utility class to emulate wix.exe with standard backends.
    /// </summary>
    public static class WixRunner
    {
        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static int Execute(string[] args, out List<Message> messages)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var task = Execute(args, serviceProvider, out messages);
            return task.Result;
        }

        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static WixRunnerResult Execute(params string[] args)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var exitCode = Execute(args, serviceProvider, out var messages);
            return new WixRunnerResult { ExitCode = exitCode.Result, Messages = messages.ToArray() };
        }

        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="coreProvider"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static Task<int> Execute(string[] args, IWixToolsetCoreServiceProvider coreProvider, out List<Message> messages)
        {
            coreProvider.AddWindowsInstallerBackend()
                        .AddBundleBackend();

            var listener = new TestMessageListener();

            messages = listener.Messages;

            var messaging = coreProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var commandLine = coreProvider.GetService<ICommandLine>();
            var command = commandLine.CreateCommand(args);
            return command?.ExecuteAsync(CancellationToken.None) ?? Task.FromResult(1);
        }
    }
}
