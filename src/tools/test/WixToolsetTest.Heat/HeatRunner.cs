// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Heat
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WixToolset.Core;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.Heat;

    /// <summary>
    /// Utility class to emulate heat.exe.
    /// </summary>
    public static class HeatRunner
    {
        /// <summary>
        /// Emulates calling heat.exe.
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
            return Execute(warningsAsErrors: false, args);
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
            var listener = new TestMessageListener();

            messages = listener.Messages;

            var messaging = coreProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            if (warningsAsErrors)
            {
                messaging.WarningsAsError = true;
            }

            var program = new Program();
            return program.Run(coreProvider, listener, args);
        }
    }
}
