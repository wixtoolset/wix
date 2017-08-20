// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data;

    public abstract class ExtensionData : IExtensionData
    {
        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>Table definisions for this extension or null if there are no table definitions.</value>
        public virtual TableDefinitionCollection TableDefinitions
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional default culture.
        /// </summary>
        /// <value>The optional default culture.</value>
        public virtual string DefaultCulture
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The library for this extension or null if there is no library.</returns>
        public virtual Library GetLibrary(TableDefinitionCollection tableDefinitions)
        {
            return null;
        }

        /// <summary>
        /// Help for loading a library from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The loaded library.</returns>
        protected static Library LoadLibraryHelper(Assembly assembly, string resourceName, TableDefinitionCollection tableDefinitions)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                UriBuilder uriBuilder = new UriBuilder(assembly.CodeBase);
                uriBuilder.Scheme = "embeddedresource";
                uriBuilder.Fragment = resourceName;

                return Library.Load(resourceStream, uriBuilder.Uri, tableDefinitions, false);
            }
        }

        /// <summary>
        /// Helper for loading table definitions from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <returns>The loaded table definitions.</returns>
        protected static TableDefinitionCollection LoadTableDefinitionHelper(Assembly assembly, string resourceName)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (XmlReader reader = XmlReader.Create(resourceStream))
            {
                return TableDefinitionCollection.Load(reader);
            }
        }
    }
}
