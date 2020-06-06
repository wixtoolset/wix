// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Loads extensions and uses the extensions' factories to provide services.
    /// </summary>
    public interface IExtensionManager
    {
        /// <summary>
        /// Adds an extension assembly directly to the manager.
        /// </summary>
        /// <param name="extensionAssembly">Extension assembly.</param>
        void Add(Assembly extensionAssembly);

        /// <summary>
        /// Loads an extension assembly from an extension reference string.
        /// </summary>
        /// <param name="extensionReference">Reference to the extension.</param>
        /// <returns>The loaded assembly. This assembly can be ignored since the extension manager maintains the list of loaded assemblies internally.</returns>
        /// <remarks>
        /// <paramref name="extensionReference"/> can be in several different forms:
        /// <list type="number">
        /// <item><term>Full path to an extension file (C:\MyExtensions\MyExtension.Example.wixext.dll)</term></item>
        /// <item><term>Reference to latest version of an extension in the cache (MyExtension.Example.wixext)</term></item>
        /// <item><term>Versioned reference to specific extension in the cache (MyExtension.Example.wixext/1.0.2)</term></item>
        /// <item><term>Relative path to an extension file (..\..\MyExtensions\MyExtension.Example.wixext.dll)</term></item>
        /// </list>
        /// </remarks>
        void Load(string extensionReference);

        /// <summary>
        /// Gets extensions of specified type from factories loaded into the extension manager.
        /// </summary>
        /// <typeparam name="T">Type of extension to get.</typeparam>
        /// <returns>Extensions of the specified type.</returns>
        IEnumerable<T> GetServices<T>() where T : class;
    }
}
