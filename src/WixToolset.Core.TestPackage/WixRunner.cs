// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    public static class WixRunner
    {
        public static int Execute(string[] args, out List<Message> messages)
        {
            var listener = new TestListener();

            messages = listener.Messages;

            var serviceProvider = new WixToolsetServiceProvider();

            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            var commandLine = serviceProvider.GetService<ICommandLineParser>();
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

        private class TestListener : IMessageListener
        {
            public List<Message> Messages { get; } = new List<Message>();

            public string ShortAppName => "TEST";

            public string LongAppName => "Test";

            public void Write(Message message)
            {
                this.Messages.Add(message);
            }

            public void Write(string message)
            {
                this.Messages.Add(new Message(null, MessageLevel.Information, 0, message));
            }
        }
    }
}
