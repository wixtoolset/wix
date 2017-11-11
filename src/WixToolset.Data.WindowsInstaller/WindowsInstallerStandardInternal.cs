// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Represents the Windows Installer standard objects.
    /// </summary>
    public static class WindowsInstallerStandardInternal
    {
        private static readonly object lockObject = new object();

        private static TableDefinitionCollection tableDefinitions;
        private static WixActionRowCollection standardActions;

        /// <summary>
        /// Gets the table definitions stored in this assembly.
        /// </summary>
        /// <returns>Table definition collection for tables stored in this assembly.</returns>
        public static TableDefinitionCollection GetTableDefinitions()
        {
            lock (lockObject)
            {
                if (null == WindowsInstallerStandardInternal.tableDefinitions)
                {
                    using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("WixToolset.Data.WindowsInstaller.Data.tables.xml")))
                    {
                        WindowsInstallerStandardInternal.tableDefinitions = TableDefinitionCollection.Load(reader);
                    }
                }
            }

            return WindowsInstallerStandardInternal.tableDefinitions;
        }

        /// <summary>
        /// Gets the standard actions stored in this assembly.
        /// </summary>
        /// <returns>Collection of standard actions in this assembly.</returns>
        public static WixActionRowCollection GetStandardActionRows()
        {
            lock (lockObject)
            {
                if (null == WindowsInstallerStandardInternal.standardActions)
                {
                    using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("WixToolset.Data.WindowsInstaller.Data.actions.xml")))
                    {
                        WindowsInstallerStandardInternal.standardActions = WixActionRowCollection.Load(reader);
                    }
                }
            }

            return WindowsInstallerStandardInternal.standardActions;
        }
    }
}
