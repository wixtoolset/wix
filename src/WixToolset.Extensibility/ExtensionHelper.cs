// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The main class for a WiX extension.
    /// </summary>
    public static class ExtensionHelper
    {
        /// <summary>
        /// Help for loading a library from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The loaded library.</returns>
        public static Library LoadLibraryHelper(Assembly assembly, string resourceName, TableDefinitionCollection tableDefinitions)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                UriBuilder uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "embeddedresource";
                uriBuilder.Path = assembly.Location;
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
        public static TableDefinitionCollection LoadTableDefinitionHelper(Assembly assembly, string resourceName)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (XmlReader reader = XmlReader.Create(resourceStream))
            {
                return TableDefinitionCollection.Load(reader);
            }
        }
    }
}
