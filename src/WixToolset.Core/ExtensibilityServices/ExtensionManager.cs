// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class ExtensionManager : IExtensionManager
    {
        private List<IExtensionFactory> extensionFactories = new List<IExtensionFactory>();
        private Dictionary<Type, List<object>> loadedExtensionsByType = new Dictionary<Type, List<object>>();

        public ExtensionManager(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public void Add(Assembly extensionAssembly)
        {
            var types = extensionAssembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && typeof(IExtensionFactory).IsAssignableFrom(t));
            var factories = types.Select(this.CreateExtensionFactory).ToList();

            this.extensionFactories.AddRange(factories);
        }

        private IExtensionFactory CreateExtensionFactory(Type type)
        {
            var constructor = type.GetConstructor(new[] { typeof(IServiceProvider) });
            if (constructor != null)
            {
                return (IExtensionFactory)constructor.Invoke(new[] { this.ServiceProvider });
            }

            return (IExtensionFactory)Activator.CreateInstance(type);
        }

        public void Load(string extensionPath)
        {
            Assembly assembly;

            // Absolute path to an assembly which means only "load from" will work even though we'd prefer to
            // use Assembly.Load (see the documentation for Assembly.LoadFrom why).
            if (Path.IsPathRooted(extensionPath))
            {
                assembly = ExtensionManager.ExtensionLoadFrom(extensionPath);
            }
            else if (ExtensionManager.TryExtensionLoad(extensionPath, out assembly))
            {
                // Loaded the assembly by name from the probing path.
            }
            else if (ExtensionManager.TryExtensionLoad(Path.GetFileNameWithoutExtension(extensionPath), out assembly))
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
                assembly = ExtensionManager.ExtensionLoadFrom(extensionPath);
            }

            this.Add(assembly);
        }

        public IEnumerable<T> Create<T>() where T : class
        {
            if (!this.loadedExtensionsByType.TryGetValue(typeof(T), out var extensions))
            {
                extensions = new List<object>();

                foreach (var factory in this.extensionFactories)
                {
                    if (factory.TryCreateExtension(typeof(T), out var obj) && obj is T extension)
                    {
                        extensions.Add(extension);
                    }
                }

                this.loadedExtensionsByType.Add(typeof(T), extensions);
            }

            return extensions.Cast<T>().ToList();
        }

        private static Assembly ExtensionLoadFrom(string assemblyName)
        {
            try
            {
                return Assembly.LoadFrom(assemblyName);
            }
            catch (Exception e)
            {
                throw new WixException(ErrorMessages.InvalidExtension(assemblyName, e.Message), e);
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

                throw new WixException(ErrorMessages.InvalidExtension(assemblyName, innerE.Message), innerE);
            }
            catch (Exception e)
            {
                throw new WixException(ErrorMessages.InvalidExtension(assemblyName, e.Message), e);
            }
        }
    }
}
