// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppExtensionData : ExtensionData
    {
        private static TableDefinitionCollection tableDefinitions;

        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>The optional table definitions for this extension.</value>
        public override TableDefinitionCollection TableDefinitions
        {
            get
            {
                return DifxAppExtensionData.GetExtensionTableDefinitions();
            }
        }

        /// <summary>
        /// Internal mechanism to access the extension's table definitions.
        /// </summary>
        /// <returns>Extension's table definitions.</returns>
        internal static TableDefinitionCollection GetExtensionTableDefinitions()
        {
            if (null == DifxAppExtensionData.tableDefinitions)
            {
                DifxAppExtensionData.tableDefinitions = ExtensionData.LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(), "WixToolset.Extensions.Data.tables.xml");
            }

            return DifxAppExtensionData.tableDefinitions;
        }
    }
}
