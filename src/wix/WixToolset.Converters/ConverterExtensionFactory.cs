// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using WixToolset.Extensibility;

    internal class ConverterExtensionFactory : IExtensionFactory
    {
        public ConverterExtensionFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public bool TryCreateExtension(Type extensionType, out object extension)
        {
            extension = null;

            if (extensionType == typeof(IExtensionCommandLine))
            {
                extension = new ConverterExtensionCommandLine(this.ServiceProvider);
            }

            return extension != null;
        }
    }
}
