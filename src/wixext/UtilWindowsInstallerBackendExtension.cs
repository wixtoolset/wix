// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class UtilWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = LoadTables();

        public override IEnumerable<TableDefinition> TableDefinitions { get => Tables; }

        private static TableDefinition[] LoadTables()
        {
            using (var resourceStream = typeof(UtilWindowsInstallerBackendBinderExtension).Assembly.GetManifestResourceStream("WixToolset.Util.tables.xml"))
            using (var reader = XmlReader.Create(resourceStream))
            {
                var tables = TableDefinitionCollection.Load(reader);
                return tables.ToArray();
            }
        }
    }
}
