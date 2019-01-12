// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the WiX Toolset UI Extension.
    /// </summary>
    public sealed class UIDecompiler : DecompilerExtension
    {
        private bool removeLibraryRows;

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return removeLibraryRows ? UIExtensionData.GetExtensionLibrary(tableDefinitions) : null;
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void Initialize(TableIndexedCollection tables)
        {
            Table propertyTable = tables["Property"];

            if (null != propertyTable)
            {
                foreach (Row row in propertyTable.Rows)
                {
                    if ("WixUI_Mode" == (string)row[0])
                    {
                        Wix.UIRef uiRef = new Wix.UIRef();

                        uiRef.Id = String.Concat("WixUI_", (string)row[1]);

                        this.Core.RootElement.AddChild(uiRef);
                        this.removeLibraryRows = true;

                        break;
                    }
                }
            }
        }
    }
}
