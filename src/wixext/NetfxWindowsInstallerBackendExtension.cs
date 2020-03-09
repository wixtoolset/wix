// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class NetfxWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = new[] {
            new TableDefinition(
                "Wix4NetFxNativeImage",
                "NetFxNativeImage",
                new[]
                {
                    new ColumnDefinition("Wix4NetFxNativeImage", ColumnType.String, 72, true, false, ColumnCategory.Identifier, description: "The primary key, a non-localized token."),
                    new ColumnDefinition("File_", ColumnType.String, 0, false, false, ColumnCategory.Identifier, keyTable:"File", keyColumn: 1, description: "The assembly for which a native image will be generated."),
                    new ColumnDefinition("Priority", ColumnType.Number, 2, false, false, ColumnCategory.Integer, maxValue: 3, description: "The priority for generating this native image: 0 is syncronous, 1-3 represent various levels of queued generation."),
                    new ColumnDefinition("Attributes", ColumnType.Number, 4, false, false, ColumnCategory.Integer, maxValue: 2147483647, description: "Integer containing bit flags representing native image attributes."),
                    new ColumnDefinition("File_Application", ColumnType.String, 72, false, true, ColumnCategory.Formatted, description: "The application which loads this assembly."),
                    new ColumnDefinition("Directory_ApplicationBase", ColumnType.String, 72, false, true, ColumnCategory.Formatted, description: "The directory containing the application which loads this assembly."),
                }
            ),
        };

        public override IEnumerable<TableDefinition> TableDefinitions { get => Tables; }

        public override bool TryAddTupleToOutput(IntermediateTuple tuple, WindowsInstallerData output)
        {
            var columnZeroIsId = tuple.Id != null;
            return this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(tuple, output, this.TableDefinitions, columnZeroIsId);
        }
    }
}
