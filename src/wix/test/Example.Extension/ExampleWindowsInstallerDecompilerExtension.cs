// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    internal class ExampleWindowsInstallerDecompilerExtension : BaseWindowsInstallerDecompilerExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => ExampleTableDefinitions.All;

        public override bool TryDecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "Wix4Example":
                    this.ProcessExampleTable(table);
                    return true;
            }

            return false;
        }

        private void ProcessExampleTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var componentId = row.FieldAsString(1);
                if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                {
                    component.Add(new XElement(ExampleConstants.ExampleName,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Value", row.FieldAsString(2))
                        ));
                }
            }
        }
    }
}
