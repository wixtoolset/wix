// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.CommandLine;
    using WixToolset.Core.ExtensibilityServices;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    public class WixToolsetServiceProvider : IServiceProvider
    {
        public WixToolsetServiceProvider()
        {
            this.CreationFunctions = new Dictionary<Type, Func<IServiceProvider, Dictionary<Type, object>, object>>
            {
            // Singletons.
                { typeof(IExtensionManager), (provider, singletons) => AddSingleton(singletons, typeof(IExtensionManager), new ExtensionManager()) },
                { typeof(IMessaging), (provider, singletons) => AddSingleton(singletons, typeof(IMessaging), new Messaging()) },
                { typeof(ITupleDefinitionCreator), (provider, singletons) => AddSingleton(singletons, typeof(ITupleDefinitionCreator), new TupleDefinitionCreator(provider)) },
                { typeof(IParseHelper), (provider, singletons) => AddSingleton(singletons, typeof(IParseHelper), new ParseHelper(provider)) },
                { typeof(IPreprocessHelper), (provider, singletons) => AddSingleton(singletons, typeof(IPreprocessHelper), new PreprocessHelper(provider)) },
                { typeof(IWindowsInstallerBackendHelper), (provider, singletons) => AddSingleton(singletons, typeof(IWindowsInstallerBackendHelper), new WindowsInstallerBackendHelper(provider)) },

            // Transients.
                { typeof(ICommandLineArguments), (provider, singletons) => new CommandLineArguments(provider) },
                { typeof(ICommandLineContext), (provider, singletons) => new CommandLineContext(provider) },
                { typeof(ICommandLineParser), (provider, singletons) => new CommandLineParser(provider) },
                { typeof(IPreprocessContext), (provider, singletons) => new PreprocessContext(provider) },
                { typeof(ICompileContext), (provider, singletons) => new CompileContext(provider) },
                { typeof(ILinkContext), (provider, singletons) => new LinkContext(provider) },
                { typeof(IResolveContext), (provider, singletons) => new ResolveContext(provider) },
                { typeof(IBindContext), (provider, singletons) => new BindContext(provider) },
                { typeof(ILayoutContext), (provider, singletons) => new LayoutContext(provider) },
                { typeof(IInscribeContext), (provider, singletons) => new InscribeContext(provider) },
            };

            this.Singletons = new Dictionary<Type, object>();
        }

        private Dictionary<Type, Func<IServiceProvider, Dictionary<Type, object>, object>> CreationFunctions { get; }

        private Dictionary<Type, object> Singletons { get; }

        public bool TryGetService(Type serviceType, out object service)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            if (!this.Singletons.TryGetValue(serviceType, out service))
            {
                if (this.CreationFunctions.TryGetValue(serviceType, out var creationFunction))
                {
                    service = creationFunction(this, this.Singletons);

#if DEBUG
                    if (!serviceType.IsAssignableFrom(service?.GetType()))
                    {
                        throw new InvalidOperationException($"Creation function for service type: {serviceType.Name} created incompatible service with type: {service?.GetType()}");
                    }
#endif
                }
            }

            return service != null;
        }

        public object GetService(Type serviceType)
        {
            return this.TryGetService(serviceType, out var service) ? service : throw new ArgumentException($"Unknown service type: {serviceType.Name}", nameof(serviceType));
        }

        public void AddService(Type serviceType, Func<IServiceProvider, Dictionary<Type, object>, object> creationFunction)
        {
            this.CreationFunctions[serviceType] = creationFunction;
        }

        private static object AddSingleton(Dictionary<Type, object> singletons, Type type, object service)
        {
            singletons.Add(type, service);
            return service;
        }
    }
}
