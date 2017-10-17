// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public class WixToolsetServiceProvider : IServiceProvider
    {
        private ExtensionManager extensionManager;

        public object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // Transients.
            if (serviceType == typeof(IBindContext))
            {
                return new BindContext(this);
            }

            if (serviceType == typeof(IInscribeContext))
            {
                return new InscribeContext(this);
            }

            if (serviceType == typeof(ICommandLineContext))
            {
                return new CommandLineContext(this);
            }

            if (serviceType == typeof(ICommandLine))
            {
                return new CommandLine();
            }

            // Singletons.
            if (serviceType == typeof(IExtensionManager))
            {
                return extensionManager = extensionManager ?? new ExtensionManager();
            }

            throw new ArgumentException($"Unknown service type: {serviceType.Name}", nameof(serviceType));
        }
    }
}
