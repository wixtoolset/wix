// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public class ExtensionManager : IExtensionManager
    {
        private List<Assembly> extensionAssemblies = new List<Assembly>();

        /// <summary>
        /// Adds an assembly.
        /// </summary>
        /// <param name="assembly">Assembly to add to the extension manager.</param>
        /// <returns>The assembly added.</returns>
        public Assembly Add(Assembly assembly)
        {
            this.extensionAssemblies.Add(assembly);
            return assembly;
        }

        /// <summary>
        /// Loads an assembly from a type description string.
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
        public Assembly Load(string extension)
        {
            string assemblyName = extension;
            Assembly assembly;

            // Absolute path to an assembly which means only "load from" will work even though we'd prefer to
            // use Assembly.Load (see the documentation for Assembly.LoadFrom why).
            if (Path.IsPathRooted(assemblyName))
            {
                assembly = ExtensionManager.ExtensionLoadFrom(assemblyName);
            }
            else if (ExtensionManager.TryExtensionLoad(assemblyName, out assembly))
            {
                // Loaded the assembly by name from the probing path.
            }
            else if (ExtensionManager.TryExtensionLoad(Path.GetFileNameWithoutExtension(assemblyName), out assembly))
            {
                // Loaded the assembly by filename alone along the probing path.
            }
            else // relative path to an assembly
            {
                // We want to use Assembly.Load when we can because it has some benefits over Assembly.LoadFrom
                // (see the documentation for Assembly.LoadFrom). However, it may fail when the path is a relative
                // path, so we should try Assembly.LoadFrom one last time. We could have detected a directory
                // separator character and used Assembly.LoadFrom directly, but dealing with path canonicalization
                // issues is something we don't want to deal with if we don't have to.
                assembly = ExtensionManager.ExtensionLoadFrom(assemblyName);
            }

            return this.Add(assembly);
        }

        /// <summary>
        /// Creates extension of specified type from assemblies loaded into the extension manager.
        /// </summary>
        /// <typeparam name="T">Type of extension to create.</typeparam>
        /// <returns>Extensions created of the specified type.</returns>
        public IEnumerable<T> Create<T>() where T : class
        {
            var types = this.extensionAssemblies.SelectMany(a => a.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && typeof(T).IsAssignableFrom(t)));
            return types.Select(t => (T)Activator.CreateInstance(t)).ToList();
        }

        private static Assembly ExtensionLoadFrom(string assemblyName)
        {
            try
            {
                return Assembly.LoadFrom(assemblyName);
            }
            catch (Exception e)
            {
                throw new WixException(WixErrors.InvalidExtension(assemblyName, e.Message), e);
            }
        }

        private static bool TryExtensionLoad(string assemblyName, out Assembly assembly)
        {
            try
            {
                assembly = Assembly.Load(assemblyName);
                return true;
            }
            catch (IOException innerE)
            {
                if (innerE is FileLoadException || innerE is FileNotFoundException)
                {
                    assembly = null;
                    return false;
                }

                throw new WixException(WixErrors.InvalidExtension(assemblyName, innerE.Message), innerE);
            }
            catch (Exception e)
            {
                throw new WixException(WixErrors.InvalidExtension(assemblyName, e.Message), e);
            }
        }
    }
}
