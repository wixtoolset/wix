// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The core of the service provider used to add services to the service provider.
    /// </summary>
    public interface IWixToolsetCoreServiceProvider : IWixToolsetServiceProvider
    {
        /// <summary>
        /// Adds a service to the service locator.
        /// </summary>
        /// <param name="serviceType">Type of the service to add.</param>
        /// <param name="creationFunction">
        /// A function that creates the service. The create function is provided the service provider
        /// itself to resolve additional services and a type dictionary that stores singleton services
        /// the creation function can add its service to.
        /// </param>
        void AddService(Type serviceType, Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, object> creationFunction);

        /// <summary>
        /// Adds a service to the service locator.
        /// </summary>
        /// <param name="serviceType">Type of the service to add.</param>
        /// <param name="creationFunction">
        /// A function that creates the service. The create function is provided the service provider
        /// itself to resolve additional services and a type dictionary that stores singleton services
        /// the creation function can add its service to.
        /// </param>
        void AddService<T>(Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, T> creationFunction) where T : class;
    }
}
