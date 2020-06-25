// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.ExtensibilityServices
{
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class WindowsInstallerBackendHelper : IWindowsInstallerBackendHelper
    {
        public Row CreateRow(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData output, TableDefinition tableDefinition)
        {
            var table = output.EnsureTable(tableDefinition);

            var row = table.CreateRow(symbol.SourceLineNumbers);
            row.SectionId = section.Id;

            return row;
        }

        public bool TryAddSymbolToOutputMatchingTableDefinitions(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            var tableDefinition = tableDefinitions.FirstOrDefault(t => t.SymbolDefinition?.Name == symbol.Definition.Name);
            if (tableDefinition == null)
            {
                return false;
            }

            var row = this.CreateRow(section, symbol, output, tableDefinition);
            var rowOffset = 0;

            if (tableDefinition.SymbolIdIsPrimaryKey)
            {
                row[0] = symbol.Id.Id;
                rowOffset = 1;
            }

            for (var i = 0; i < symbol.Fields.Length; ++i)
            {
                if (i < tableDefinition.Columns.Length)
                {
                    var column = tableDefinition.Columns[i + rowOffset];

                    switch (column.Type)
                    {
                    case ColumnType.Number:
                        row[i + rowOffset] = column.Nullable ? symbol.AsNullableNumber(i) : symbol.AsNumber(i);
                        break;

                    default:
                        row[i + rowOffset] = symbol.AsString(i);
                        break;
                    }
                }
            }

            return true;
        }
    }
}
