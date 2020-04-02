// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class DecompileContext : IDecompileContext
    {
        internal DecompileContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public string DecompilePath { get; set; }

        public OutputType DecompileType { get; set; }

        public IEnumerable<IDecompilerExtension> Extensions { get; set; }

        public string ExtractFolder { get; set; }

        public string CabinetExtractFolder { get; set; }

        public string BaseSourcePath { get; set; }

        public string IntermediateFolder { get; set; }

        public bool IsAdminImage { get; set; }

        public string OutputPath { get; set; }

        public bool SuppressCustomTables { get; set; }

        public bool SuppressDroppingEmptyTables { get; set; }

        public bool SuppressExtractCabinets { get; set; }

        public bool SuppressUI { get; set; }

        public bool TreatProductAsModule { get; set; }
    }
}
