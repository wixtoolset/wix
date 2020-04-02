// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;

    public interface IWixToolsetServiceProvider : IServiceProvider
    {
        bool TryGetService(Type serviceType, out object service);
        bool TryGetService<T>(out T service) where T : class;
        T GetService<T>() where T : class;
    }
}
