// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;

    /// <summary>
    /// Service provider extensions.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <typeparam name="T">Type of service to get.</typeparam>
        /// <param name="provider">Service provider.</param>
        public static T GetService<T>(this IServiceProvider provider) where T : class
        {
            return provider.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <param name="provider">Service provider.</param>
        /// <param name="serviceType">Type of service to get.</param>
        /// <param name="service">Retrieved service.</param>
        /// <returns>True if the service was found, otherwise false</returns>
        public static bool TryGetService(this IServiceProvider provider, Type serviceType, out object service)
        {
            service = provider.GetService(serviceType);
            return service != null;
        }

        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <typeparam name="T">Type of service to get.</typeparam>
        /// <param name="provider">Service provider.</param>
        /// <param name="service">Retrieved service.</param>
        /// <returns>True if the service was found, otherwise false</returns>
        public static bool TryGetService<T>(this IServiceProvider provider, out T service) where T : class
        {
            service = provider.GetService(typeof(T)) as T;
            return service != null;
        }
    }
}
