// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;

    /// <summary>
    /// Service provider.
    /// </summary>
    public interface IWixToolsetServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <typeparam name="T">Type of service to get.</typeparam>
        T GetService<T>() where T : class;

        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <param name="serviceType">Type of service to get.</param>
        /// <param name="service">Retrieved service.</param>
        /// <returns>True if the service was found, otherwise false</returns>
        bool TryGetService(Type serviceType, out object service);

        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <typeparam name="T">Type of service to get.</typeparam>
        /// <param name="service">Retrieved service.</param>
        /// <returns>True if the service was found, otherwise false</returns>
        bool TryGetService<T>(out T service) where T : class;
    }
}
