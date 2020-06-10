// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    public partial class WixBuild
    {
        protected override string TaskShortName => "WIX";

        protected override Task<int> ExecuteCoreAsync(IWixToolsetCoreServiceProvider serviceProvider, string commandLineString, CancellationToken cancellationToken)
        {
            var messaging = serviceProvider.GetService<IMessaging>();

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(commandLineString);

            var commandLine = serviceProvider.GetService<ICommandLine>();
            commandLine.ExtensionManager = this.CreateExtensionManagerWithStandardBackends(serviceProvider, messaging, arguments.Extensions);
            commandLine.Arguments = arguments;
            var command = commandLine.ParseStandardCommandLine();
            return command?.ExecuteAsync(cancellationToken) ?? Task.FromResult(1);
        }

        private IExtensionManager CreateExtensionManagerWithStandardBackends(IWixToolsetServiceProvider serviceProvider, IMessaging messaging, string[] extensions)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            foreach (var extension in extensions)
            {
                try
                {
                    extensionManager.Load(extension);
                }
                catch (WixException e)
                {
                    messaging.Write(e.Error);
                }
            }

            return extensionManager;
        }
    }
}
#endif
