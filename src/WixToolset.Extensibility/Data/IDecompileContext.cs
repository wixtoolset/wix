// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

#pragma warning disable 1591 // TODO: add documentation
    public interface IDecompileContext
    {
        IServiceProvider ServiceProvider { get; }

        string DecompilePath { get; set; }

        OutputType DecompileType { get; set; }

        IEnumerable<IDecompilerExtension> Extensions { get; set; }

        string ExtractFolder { get; set; }

        string CabinetExtractFolder { get; set; }

        /// <summary>
        /// Optional gets or sets the base path for the File/@Source.
        /// </summary>
        /// <remarks>Default value is "SourceDir" to enable use of BindPaths.</remarks>
        string BaseSourcePath { get; set; }

        string IntermediateFolder { get; set; }

        bool IsAdminImage { get; set; }

        string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress custom tables.
        /// </summary>
        bool SuppressCustomTables { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress dropping empty tables.
        /// </summary>
        bool SuppressDroppingEmptyTables { get; set; }

        bool SuppressExtractCabinets { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress decompiling UI-related tables.
        /// </summary>
        bool SuppressUI { get; set; }

        /// <summary>
        /// Gets or sets whether the decompiler should use module logic on a product output.
        /// </summary>
        bool TreatProductAsModule { get; set; }
    }
}
