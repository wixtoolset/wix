// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class WindowsInstallerBackendHelper : IWindowsInstallerBackendHelper
    {
        public Row CreateRow(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinition tableDefinition)
        {
            var table = output.EnsureTable(tableDefinition);

            var row = table.CreateRow(tuple.SourceLineNumbers);
            row.SectionId = section.Id;

            return row;
        }

        public bool TryAddTupleToOutputMatchingTableDefinitions(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            var tableDefinition = tableDefinitions.FirstOrDefault(t => t.TupleDefinitionName == tuple.Definition.Name);
            if (tableDefinition == null)
            {
                return false;
            }

            var row = this.CreateRow(section, tuple, output, tableDefinition);
            var rowOffset = 0;

            if (tableDefinition.TupleIdIsPrimaryKey)
            {
                row[0] = tuple.Id.Id;
                rowOffset = 1;
            }

            for (var i = 0; i < tuple.Fields.Length; ++i)
            {
                if (i < tableDefinition.Columns.Length)
                {
                    var column = tableDefinition.Columns[i + rowOffset];

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
