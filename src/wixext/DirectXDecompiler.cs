// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The WiX Toolset DirectX Extension.
    /// </summary>
    public sealed class DirectXDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return DirectXExtensionData.GetExtensionLibrary(tableDefinitions);
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
                    if ("SecureCustomProperties" == row[0].ToString())
                    {
                        // if we've referenced any of the DirectX properties, add
                        // a PropertyRef to pick up the CA from the extension and then remove
                        // it from the SecureCustomExtensions property so we don't get duplicates
                        StringBuilder remainingProperties = new StringBuilder();
                        string[] secureCustomProperties = row[1].ToString().Split(';');
                        foreach (string property in secureCustomProperties)
                        {
                            if (property.StartsWith("WIX_DIRECTX_"))
                            {
                                Wix.PropertyRef propertyRef = new Wix.PropertyRef();
                                propertyRef.Id = property;
                                this.Core.RootElement.AddChild(propertyRef);
                            }
                            else
                            {
                                if (0 < remainingProperties.Length)
                                {
                                    remainingProperties.Append(";");
                                }
                                remainingProperties.Append(property);
                            }
                        }

                        row[1] = remainingProperties.ToString();
                        break;
                    }
                }
            }
        }
    }
}
