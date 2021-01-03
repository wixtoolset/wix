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
        /// <param name="warningsAsErrors"></param>
        /// <returns></returns>
        public static int Execute(string[] args, out List<Message> messages, bool warningsAsErrors = true)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var task = Execute(args, serviceProvider, out messages, warningsAsErrors: warningsAsErrors);
            return task.Result;
        }

        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// This overload always treats warnings as errors.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static WixRunnerResult Execute(params string[] args)
        {
            return Execute(true, args);
        }

        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// </summary>
        /// <param name="warningsAsErrors"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static WixRunnerResult Execute(bool warningsAsErrors, params string[] args)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var exitCode = Execute(args, serviceProvider, out var messages, warningsAsErrors: warningsAsErrors);
            return new WixRunnerResult { ExitCode = exitCode.Result, Messages = messages.ToArray() };
        }

        /// <summary>
        /// Emulates calling wix.exe with standard backends.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="coreProvider"></param>
        /// <param name="messages"></param>
        /// <param name="warningsAsErrors"></param>
        /// <returns></returns>
        public static Task<int> Execute(string[] args, IWixToolsetCoreServiceProvider coreProvider, out List<Message> messages, bool warningsAsErrors = true)
        {
            coreProvider.AddWindowsInstallerBackend()
                        .AddBundleBackend();

            var listener = new TestMessageListener();

            messages = listener.Messages;

            var messaging = coreProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = new List<string>(args);
            if (warningsAsErrors)
            {
                arguments.Add("-wx");
            }

            var commandLine = coreProvider.GetService<ICommandLine>();
            var command = commandLine.CreateCommand(arguments.ToArray());
            return command?.ExecuteAsync(CancellationToken.None) ?? Task.FromResult(1);
        }
    }
}
