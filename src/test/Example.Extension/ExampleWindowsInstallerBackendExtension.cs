// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    internal class ExampleWindowsInstallerBackendExtension : BaseWindowsInstallerBackendBinderExtension
    {
        public override IEnumerable<TableDefinition> TableDefinitions => ExampleTableDefinitions.All;

        public override bool TryAddTupleToOutput(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            if (ExampleTupleDefinitions.TryGetTupleType(tuple.Definition.Name, out var tupleType))
            {
                switch (tupleType)
                {
                    case ExampleTupleDefinitionType.Example:
                        {
                            var row = (ExampleRow)this.BackendHelper.CreateRow(section, tuple, output, ExampleTableDefinitions.ExampleTable);
                            row.Example = tuple.Id.Id;
                            row.Value = tuple[0].AsString();
                        }
                        return true;
                }
            }

            return this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(section, tuple, output, tableDefinitions);
        }
    }
}
