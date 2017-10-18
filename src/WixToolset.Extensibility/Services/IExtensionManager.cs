// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using System.Reflection;

    public interface IExtensionManager
    {
        Assembly Add(Assembly assembly);

        Assembly Load(string extension);

        IEnumerable<T> Create<T>() where T : class;
    }
}
