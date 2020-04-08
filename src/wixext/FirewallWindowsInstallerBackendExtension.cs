// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class FirewallWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = LoadTables();

        public override IEnumerable<TableDefinition> TableDefinitions => Tables;

        public override bool TryAddTupleToOutput(IntermediateTuple tuple, WindowsInstallerData output) => this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(tuple, output, this.TableDefinitionsForTuples, true);

        private static TableDefinition[] LoadTables()
        {
            using (var resourceStream = typeof(FirewallWindowsInstallerBackendBinderExtension).Assembly.GetManifestResourceStream("WixToolset.Firewall.tables.xml"))
            using (var reader = XmlReader.Create(resourceStream))
            {
                var tables = TableDefinitionCollection.Load(reader);
                return tables.ToArray();
            }
        }
    }
}
