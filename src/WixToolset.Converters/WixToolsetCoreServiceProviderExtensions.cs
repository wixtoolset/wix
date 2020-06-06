// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using WixToolset.Extensibility.Services;

    public static class WixToolsetCoreServiceProviderExtensions
    {
        public static IWixToolsetCoreServiceProvider AddConverter(this IWixToolsetCoreServiceProvider serviceProvider)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(ConverterExtensionFactory).Assembly);

            return serviceProvider;
        }
    }
}
