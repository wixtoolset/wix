// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class HttpWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = LoadTables();

        public override IEnumerable<TableDefinition> TableDefinitions => Tables;

        private static TableDefinition[] LoadTables()
        {
            using (var resourceStream = typeof(HttpWindowsInstallerBackendBinderExtension).Assembly.GetManifestResourceStream("WixToolset.Http.tables.xml"))
            using (var reader = XmlReader.Create(resourceStream))
            {
                var tables = TableDefinitionCollection.Load(reader);
                return tables.ToArray();
            }
        }
    }
}
