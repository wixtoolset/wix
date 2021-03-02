// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.ExtensibilityServices
{
    using System;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class WindowsInstallerBackendHelper : IWindowsInstallerBackendHelper
    {
        private readonly IBackendHelper backendHelper;

        public WindowsInstallerBackendHelper(IWixToolsetServiceProvider serviceProvider)
        {
            this.backendHelper = serviceProvider.GetService<IBackendHelper>();
        }

        #region IBackendHelper interfaces

        public IFileTransfer CreateFileTransfer(string source, string destination, bool move, SourceLineNumber sourceLineNumbers = null) => this.backendHelper.CreateFileTransfer(source, destination, move, sourceLineNumbers);

        public string CreateGuid(Guid namespaceGuid, string value) => this.backendHelper.CreateGuid(namespaceGuid, value);

        public IResolvedDirectory CreateResolvedDirectory(string directoryParent, string name) => this.backendHelper.CreateResolvedDirectory(directoryParent, name);

        public string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath) => this.backendHelper.GetCanonicalRelativePath(sourceLineNumbers, elementName, attributeName, relativePath);

        public ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null) => this.backendHelper.TrackFile(path, type, sourceLineNumbers);

        #endregion

        #region IWindowsInstallerBackendHelper interfaces

        public Row CreateRow(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData data, TableDefinition tableDefinition)
        {
            var table = data.EnsureTable(tableDefinition);

            var row = table.CreateRow(symbol.SourceLineNumbers);
            row.SectionId = section.Id;

            return row;
        }

        public bool TryAddSymbolToMatchingTableDefinitions(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData data, TableDefinitionCollection tableDefinitions)
        {
            var tableDefinition = tableDefinitions.FirstOrDefault(t => t.SymbolDefinition?.Name == symbol.Definition.Name);
            if (tableDefinition == null)
            {
                return false;
            }

            var row = this.CreateRow(section, symbol, data, tableDefinition);
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

        #endregion
    }
}
