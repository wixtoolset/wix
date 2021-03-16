// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Burn;
    using WixToolset.Core.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    public partial class WixBuild
    {
        protected override string TaskShortName => "WIX";

        protected override Task<int> ExecuteCoreAsync(IWixToolsetCoreServiceProvider coreProvider, string commandLineString, CancellationToken cancellationToken)
        {
            coreProvider.AddWindowsInstallerBackend()
                        .AddBundleBackend();

            var commandLine = coreProvider.GetService<ICommandLine>();
            var command = commandLine.CreateCommand(commandLineString);
            return command?.ExecuteAsync(cancellationToken) ?? Task.FromResult(1);
        }
    }
}
#endif
