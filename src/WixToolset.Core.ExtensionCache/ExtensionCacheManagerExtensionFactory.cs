// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class ExtensionCacheManagerExtensionFactory : IExtensionFactory
    {
        public ExtensionCacheManagerExtensionFactory(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        public bool TryCreateExtension(Type extensionType, out object extension)
        {
            extension = null;

            if (extensionType == typeof(IExtensionCommandLine))
            {
                extension = new ExtensionCacheManagerExtensionCommandLine(this.ServiceProvider);
            }

            return extension != null;
        }
    }
}
