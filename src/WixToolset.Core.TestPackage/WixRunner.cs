// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    public static class WixRunner
    {
        public static int Execute(string[] args, out List<Message> messages)
        {
            var serviceProvider = new WixToolsetServiceProvider();
            return Execute(args, serviceProvider, out messages);
        }

        public static WixRunnerResult Execute(string[] args)
        {
            var serviceProvider = new WixToolsetServiceProvider();
            var exitCode = Execute(args, serviceProvider, out var messages);
            return new WixRunnerResult { ExitCode = exitCode, Messages = messages.ToArray() };
        }

        public static int Execute(string[] args, IServiceProvider serviceProvider, out List<Message> messages)
        {
            var listener = new TestMessageListener();

            messages = listener.Messages;

            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            var commandLine = serviceProvider.GetService<ICommandLine>();
            commandLine.ExtensionManager = CreateExtensionManagerWithStandardBackends(serviceProvider, arguments.Extensions);
            commandLine.Arguments = arguments;
            var command = commandLine.ParseStandardCommandLine();
            return command?.Execute() ?? 1;
        }

        private static IExtensionManager CreateExtensionManagerWithStandardBackends(IServiceProvider serviceProvider, string[] extensions)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            foreach (var extension in extensions)
            {
                extensionManager.Load(extension);
            }

            return extensionManager;
        }
    }
}
