// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for extension factories.
    ///
    /// Implementations may request an IWixToolsetCoreServiceProvider at instantiation by having a single parameter constructor for it.
    /// </summary>
    public abstract class BaseExtensionFactory : IExtensionFactory
    {
        /// <summary>
        /// The extension types of the WiX extension.
        /// </summary>
        protected abstract IReadOnlyCollection<Type> ExtensionTypes { get; }

        /// <summary>
        /// See <see cref="IExtensionFactory.TryCreateExtension(Type, out object)"/>
        /// </summary>
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
