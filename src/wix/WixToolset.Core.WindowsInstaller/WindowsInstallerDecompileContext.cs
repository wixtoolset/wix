// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class WindowsInstallerDecompileContext : IWindowsInstallerDecompileContext
    {
        internal WindowsInstallerDecompileContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public string DecompilePath { get; set; }

        public OutputType DecompileType { get; set; }

        public IReadOnlyCollection<IWindowsInstallerDecompilerExtension> Extensions { get; set; }

        public IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        public ISymbolDefinitionCreator SymbolDefinitionCreator { get; set; }

        public string ExtractFolder { get; set; }

        public string CabinetExtractFolder { get; set; }

        public string BaseSourcePath { get; set; }

        public string IntermediateFolder { get; set; }

        public bool IsAdminImage { get; set; }

        public string OutputPath { get; set; }

        public bool SuppressCustomTables { get; set; }

        public bool SuppressDroppingEmptyTables { get; set; }

        public bool SuppressRelativeActionSequencing { get; set; }

        public bool SuppressExtractCabinets { get; set; }

        public bool SuppressUI { get; set; }

        public bool KeepModularizationIds { get; set; }
    }
}
