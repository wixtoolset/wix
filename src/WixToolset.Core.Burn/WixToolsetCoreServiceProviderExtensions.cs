// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Extensibility.Services;

    public static class WixToolsetCoreServiceProviderExtensions
    {
        public static IWixToolsetCoreServiceProvider AddBundleBackend(this IWixToolsetCoreServiceProvider coreProvider)
        {
            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(BurnExtensionFactory).Assembly);

            return coreProvider;
        }
    }
}
