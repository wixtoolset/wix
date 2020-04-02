// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.Collections.Generic;

    public interface IWixToolsetCoreServiceProvider : IWixToolsetServiceProvider
    {
        void AddService(Type serviceType, Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, object> creationFunction);
        void AddService<T>(Func<IWixToolsetCoreServiceProvider, Dictionary<Type, object>, T> creationFunction) where T : class;
    }
}
