// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Class for creating <see cref="IWixToolsetCoreServiceProvider"/>.
    /// </summary>
    public static class WixToolsetServiceProviderFactory
    {
        /// <summary>
        /// Creates a new <see cref="IWixToolsetCoreServiceProvider"/>.
        /// </summary>
        /// <returns>The created <see cref="IWixToolsetCoreServiceProvider"/></returns>
        public static IWixToolsetCoreServiceProvider CreateServiceProvider()
        {
            return new WixToolsetServiceProvider();
        }
    }
}
