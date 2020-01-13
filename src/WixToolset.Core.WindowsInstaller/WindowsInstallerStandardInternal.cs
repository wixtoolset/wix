// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Represents the Windows Installer standard objects.
    /// </summary>
    internal static class WindowsInstallerStandardInternal
    {
        private static readonly object lockObject = new object();

        private static TableDefinitionCollection tableDefinitions;

        /// <summary>
        /// Gets the table definitions stored in this assembly.
        /// </summary>
        /// <returns>Table definition collection for tables stored in this assembly.</returns>
        public static TableDefinitionCollection GetTableDefinitions()
        {
            // TODO: make the data static data structures instead of parsing an XML file and consider
            //       moving it all to WixToolset.Data.WindowsInstallerStandard class.
            lock (lockObject)
            {
                if (null == WindowsInstallerStandardInternal.tableDefinitions)
                {
                    using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("WixToolset.Core.WindowsInstaller.Data.tables.xml")))
                    {
                        WindowsInstallerStandardInternal.tableDefinitions = TableDefinitionCollection.Load(reader);
                    }
                }
            }

            return WindowsInstallerStandardInternal.tableDefinitions;
        }
    }
}
