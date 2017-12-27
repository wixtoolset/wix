// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for extension factories.
    /// </summary>
    public abstract class BaseExtensionFactory : IExtensionFactory
    {
        protected abstract IEnumerable<Type> ExtensionTypes { get; }

        public virtual bool TryCreateExtension(Type extensionType, out object extension)
        {
            extension = null;

            foreach (var type in this.ExtensionTypes)
            {
                if (extensionType.IsAssignableFrom(type))
                {
                    extension = Activator.CreateInstance(type);
                    break;
                }
            }

            return extension != null;
        }
    }
}
