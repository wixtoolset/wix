// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    internal class ExampleWindowsInstallerBackendExtension : BaseWindowsInstallerBackendBinderExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => ExampleTableDefinitions.All;

        public override bool TryProcessSymbol(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            if (ExampleSymbolDefinitions.TryGetSymbolType(symbol.Definition.Name, out var symbolType))
            {
                switch (symbolType)
                {
                    case ExampleSymbolDefinitionType.Example:
                        {
                            var row = (ExampleRow)this.BackendHelper.CreateRow(section, symbol, output, ExampleTableDefinitions.ExampleTable);
                            row.Example = symbol.Id.Id;
                            row.Value = symbol[0].AsString();
                        }
                        return true;
                }
            }

            return base.TryProcessSymbol(section, symbol, output, tableDefinitions);
        }
    }
}
