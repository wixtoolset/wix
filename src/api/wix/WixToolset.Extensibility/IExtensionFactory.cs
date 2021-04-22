// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;

    /// <summary>
    /// Implementations may request an IWixToolsetCoreServiceProvider at instantiation by having a single parameter constructor for it.
    /// </summary>
    public interface IExtensionFactory
    {
        /// <summary>
        /// Request to create an extension of the specified type.
        /// </summary>
        /// <param name="extensionType">Extension type to create.</param>
        /// <param name="extension">Extension created.</param>
        /// <returns>True if extension was created; otherwise false.</returns>
        bool TryCreateExtension(Type extensionType, out object extension);
    }
}
