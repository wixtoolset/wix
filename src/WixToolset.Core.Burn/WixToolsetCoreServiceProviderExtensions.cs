// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.Burn.ExtensibilityServices;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extensions methods for adding Burn services.
    /// </summary>
    public static class WixToolsetCoreServiceProviderExtensions
    {
        /// <summary>
        /// Adds Burn Services.
        /// </summary>
        /// <param name="coreProvider"></param>
        /// <returns></returns>
        public static IWixToolsetCoreServiceProvider AddBundleBackend(this IWixToolsetCoreServiceProvider coreProvider)
        {
            AddServices(coreProvider);

            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(BurnExtensionFactory).Assembly);

            return coreProvider;
        }

        private static void AddServices(IWixToolsetCoreServiceProvider coreProvider)
        {
            // Singletons.
            coreProvider.AddService((provider, singletons) => AddSingleton<IInternalBurnBackendHelper>(singletons, new BurnBackendHelper()));
            coreProvider.AddService((provider, singletons) => AddSingleton<IBurnBackendHelper>(singletons, provider.GetService<IInternalBurnBackendHelper>()));
        }

        private static T AddSingleton<T>(Dictionary<Type, object> singletons, T service) where T : class
        {
            singletons.Add(typeof(T), service);
            return service;
        }
    }
}
