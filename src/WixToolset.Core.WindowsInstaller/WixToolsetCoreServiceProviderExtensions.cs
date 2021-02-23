// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.WindowsInstaller.ExtensibilityServices;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extensions methods for adding WindowsInstaller services.
    /// </summary>
    public static class WixToolsetCoreServiceProviderExtensions
    {
        /// <summary>
        /// Adds WindowsInstaller services.
        /// </summary>
        /// <param name="coreProvider"></param>
        /// <returns></returns>
        public static IWixToolsetCoreServiceProvider AddWindowsInstallerBackend(this IWixToolsetCoreServiceProvider coreProvider)
        {
            AddServices(coreProvider);

            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(WindowsInstallerExtensionFactory).Assembly);

            return coreProvider;
        }

        private static void AddServices(IWixToolsetCoreServiceProvider coreProvider)
        {
            // Singletons.
            coreProvider.AddService((provider, singletons) => AddSingleton<IWindowsInstallerBackendHelper>(singletons, new WindowsInstallerBackendHelper(provider)));
            coreProvider.AddService<IUnbinder>((provider, singletons) => new Unbinder(provider));
        }

        private static T AddSingleton<T>(Dictionary<Type, object> singletons, T service) where T : class
        {
            singletons.Add(typeof(T), service);
            return service;
        }
    }
}
