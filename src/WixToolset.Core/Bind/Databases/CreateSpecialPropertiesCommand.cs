// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class CreateSpecialPropertiesCommand : ICommand
    {
        public Table PropertyTable { private get; set; }

        public Table WixPropertyTable { private get; set; }

        public void Execute()
        {
            // Create the special properties.
            if (null != this.WixPropertyTable)
            {
                // Create lists of the properties that contribute to the special lists of properties.
                SortedSet<string> adminProperties = new SortedSet<string>();
                SortedSet<string> secureProperties = new SortedSet<string>();
                SortedSet<string> hiddenProperties = new SortedSet<string>();

                foreach (WixPropertyRow wixPropertyRow in this.WixPropertyTable.Rows)
                {
                    if (wixPropertyRow.Admin)
                    {
                        adminProperties.Add(wixPropertyRow.Id);
                    }

                    if (wixPropertyRow.Hidden)
                    {
                        hiddenProperties.Add(wixPropertyRow.Id);
                    }

                    if (wixPropertyRow.Secure)
                    {
                        secureProperties.Add(wixPropertyRow.Id);
                    }
                }

                Table propertyTable = this.PropertyTable;
                if (0 < adminProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "AdminProperties";
                    row.Value = String.Join(";", adminProperties);
                }

                if (0 < secureProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "SecureCustomProperties";
                    row.Value = String.Join(";", secureProperties);
                }

                if (0 < hiddenProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "MsiHiddenProperties";
                    row.Value = String.Join(";", hiddenProperties);
                }
            }
        }
    }
}
