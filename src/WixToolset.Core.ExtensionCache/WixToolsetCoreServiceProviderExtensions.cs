// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility.Services;

    public static class WixToolsetCoreServiceProviderExtensions
    {
        public static IWixToolsetCoreServiceProvider AddExtensionCacheManager(this IWixToolsetCoreServiceProvider serviceProvider)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(ExtensionCacheManagerExtensionFactory).Assembly);

            serviceProvider.AddService(CreateExtensionCacheManager);
            return serviceProvider;
        }

        private static ExtensionCacheManager CreateExtensionCacheManager(IWixToolsetCoreServiceProvider provider, Dictionary<Type, object> singletons)
        {
            var extensionCacheManager = new ExtensionCacheManager();
            singletons.Add(typeof(ExtensionCacheManager), extensionCacheManager);

            return extensionCacheManager;
        }
    }
}
