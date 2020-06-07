// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters.Mocks
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility.Services;

    public class MockCoreServiceProvider : IWixToolsetCoreServiceProvider
    {
        public Dictionary<Type, Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, object>> CreationFunctions { get; } = new Dictionary<Type, Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, object>>();

        public Dictionary<Type, object> Singletons { get; } = new Dictionary<Type, object>()
        {
            { typeof(IMessaging), new MockMessaging() }
        };

        public void AddService(Type serviceType, Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, object> creationFunction) => this.CreationFunctions.Add(serviceType, creationFunction);

        public void AddService<T>(Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, T> creationFunction) where T : class => this.AddService(typeof(T), creationFunction);

        public T GetService<T>() where T : class => this.TryGetService(typeof(T), out var obj) ? (T)obj : null;

        public object GetService(Type serviceType) => this.TryGetService(serviceType, out var service) ? service : null;

        public bool TryGetService(Type serviceType, out object service)
        {
            if (!this.Singletons.TryGetValue(serviceType, out service))
            {
                if (this.CreationFunctions.TryGetValue(serviceType, out var creationFunction))
                {
                    service = creationFunction(this, this.Singletons);
                }
            }

            return service != null;
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            service = null;

            if (this.TryGetService(typeof(T), out var obj))
            {
                service = (T)obj;
            }

            return service != null;
        }
    }
}