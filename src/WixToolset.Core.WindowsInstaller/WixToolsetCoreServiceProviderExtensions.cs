// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using WixToolset.Extensibility.Services;

    public static class WixToolsetCoreServiceProviderExtensions
    {
        public static IWixToolsetCoreServiceProvider AddWindowsInstallerBackend(this IWixToolsetCoreServiceProvider coreProvider)
        {
            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(WindowsInstallerExtensionFactory).Assembly);

            return coreProvider;
        }
    }
}
