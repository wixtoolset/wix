// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;
    using System.Reflection;

    public interface IExtensionManager
    {
        /// <summary>
        /// Adds an extension assembly directly to the manager.
        /// </summary>
        /// <param name="extensionAssembly">Extension assembly.</param>
        void Add(Assembly extensionAssembly);

        /// <summary>
        /// Loads an extension assembly from a type description string.
        /// </summary>
        /// <param name="extension">The assembly type description string.</param>
        /// <returns>The loaded assembly. This assembly can be ignored since the extension manager maintains the list of loaded assemblies internally.</returns>
        /// <remarks>
        /// <paramref name="extension"/> can be in several different forms:
        /// <list type="number">
        /// <item><term>AssemblyName (MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089)</term></item>
        /// <item><term>Absolute path to an assembly (C:\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// <item><term>Filename of an assembly in the application directory (ExtensionAssembly.dll)</term></item>
        /// <item><term>Relative path to an assembly (..\..\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// </list>
        /// </remarks>
        void Load(string extensionPath);

        /// <summary>
        /// Gets extensions of specified type from factories loaded into the extension manager.
        /// </summary>
        /// <typeparam name="T">Type of extension to get.</typeparam>
        /// <returns>Extensions of the specified type.</returns>
        IEnumerable<T> GetServices<T>() where T : class;
    }
}
