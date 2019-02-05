// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using System.Linq;
    using System.Xml;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class ComPlusWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = LoadTables();

        protected override TableDefinition[] TableDefinitionsForTuples => Tables;

        private static TableDefinition[] LoadTables()
        {
            using (var resourceStream = typeof(ComPlusWindowsInstallerBackendBinderExtension).Assembly.GetManifestResourceStream("WixToolset.ComPlus.tables.xml"))
            using (var reader = XmlReader.Create(resourceStream))
            {
                var tables = TableDefinitionCollection.Load(reader);
                return tables.ToArray();
            }
        }
    }
}
