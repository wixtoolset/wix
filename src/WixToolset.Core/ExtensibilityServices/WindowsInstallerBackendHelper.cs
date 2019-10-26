// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class WindowsInstallerBackendHelper : IWindowsInstallerBackendHelper
    {
        public bool TryAddTupleToOutputMatchingTableDefinitions(IntermediateTuple tuple, WindowsInstallerData output, TableDefinition[] tableDefinitions) => this.TryAddTupleToOutputMatchingTableDefinitions(tuple, output, tableDefinitions, false);

        public bool TryAddTupleToOutputMatchingTableDefinitions(IntermediateTuple tuple, WindowsInstallerData output, TableDefinition[] tableDefinitions, bool columnZeroIsId)
        {
            var tableDefinition = tableDefinitions.FirstOrDefault(t => t.Name == tuple.Definition.Name);

            if (tableDefinition == null)
            {
                return false;
            }

            var table = output.EnsureTable(tableDefinition);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            var rowOffset = 0;

            if (columnZeroIsId)
            {
                row[0] = tuple.Id.Id;
                rowOffset = 1;
            }

            for (var i = 0; i < tuple.Fields.Length; ++i)
            {
                if (i < tableDefinition.Columns.Length)
                {
                    var column = tableDefinition.Columns[i];

                    switch (column.Type)
                    {
                    case ColumnType.Number:
                        row[i + rowOffset] = column.Nullable ? tuple.AsNullableNumber(i) : tuple.AsNumber(i);
                        break;

                    default:
                        row[i + rowOffset] = tuple.AsString(i);
                        break;
                    }
                }
            }

            return true;
        }
    }
}
