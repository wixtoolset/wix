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

        public override bool TryAddTupleToOutput(IntermediateTuple tuple, WindowsInstallerData output)
        {
#if ALTERNATIVE_TO_USING_HELPER
            switch (tuple.Definition.Name)
            {
                case TupleDefinitions.ExampleName:
                    {
                        var table = output.EnsureTable(ExampleTableDefinitions.ExampleTable);
                        var row = table.CreateRow(tuple.SourceLineNumbers);
                        row[0] = tuple[0].AsString();
                        row[1] = tuple[1].AsString();
                    }
                    return true;
            }

            return false;
#else
            return this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(tuple, output, ExampleTableDefinitions.All);
#endif
        }
    }
}
