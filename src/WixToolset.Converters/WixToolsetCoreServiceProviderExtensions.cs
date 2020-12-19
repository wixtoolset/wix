// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extension methods for adding Converters services.
    /// </summary>
    public static class WixToolsetCoreServiceProviderExtensions
    {
        /// <summary>
        /// Adds Converters services.
        /// </summary>
        /// <param name="coreProvider"></param>
        /// <returns></returns>
        public static IWixToolsetCoreServiceProvider AddConverter(this IWixToolsetCoreServiceProvider coreProvider)
        {
            var extensionManager = coreProvider.GetService<IExtensionManager>();
            extensionManager.Add(typeof(ConverterExtensionFactory).Assembly);

            return coreProvider;
        }
    }
}
