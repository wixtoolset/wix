// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extensions methods for adding ExtensionCache services.
    /// </summary>
    public static class WixToolsetCoreServiceProviderExtensions
    {
        /// <summary>
        /// Adds ExtensionCache services.
        /// </summary>
        /// <param name="coreProvider"></param>
        /// <returns></returns>
        public static IWixToolsetCoreServiceProvider AddExtensionCacheManager(this IWixToolsetCoreServiceProvider coreProvider)
        {
            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(ExtensionCacheManagerExtensionFactory).Assembly);

            coreProvider.AddService(CreateExtensionCacheManager);
            return coreProvider;
        }

        private static ExtensionCacheManager CreateExtensionCacheManager(IWixToolsetCoreServiceProvider coreProvider, Dictionary<Type, object> singletons)
        {
            var extensionCacheManager = new ExtensionCacheManager();
            singletons.Add(typeof(ExtensionCacheManager), extensionCacheManager);

            return extensionCacheManager;
        }
    }
}
