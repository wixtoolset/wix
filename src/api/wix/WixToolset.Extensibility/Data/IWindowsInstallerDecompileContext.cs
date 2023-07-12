// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// The context used to decompile a Windows Installer database.
    /// </summary>
    public interface IWindowsInstallerDecompileContext
    {
        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets or sets the path to the file to decompile.
        /// </summary>
        string DecompilePath { get; set; }

        /// <summary>
        /// Gets or sets the type to decompile.
        /// </summary>
        OutputType DecompileType { get; set; }

        /// <summary>
        /// Gets or sets the decompiler extensions.
        /// </summary>
        IReadOnlyCollection<IWindowsInstallerDecompilerExtension> Extensions { get; set; }

        /// <summary>
        /// Collection of extension data to use during decompiling.
        /// </summary>
        IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        /// <summary>
        /// Symbol definition creator used to load extension data.
        /// </summary>
        ISymbolDefinitionCreator SymbolDefinitionCreator { get; set; }

        /// <summary>
        /// Gets or sets the folder where content is extracted.
        /// </summary>
        string ExtractFolder { get; set; }

        /// <summary>
        /// Gets or sets the folder where files are extracted.
        /// </summary>
        string CabinetExtractFolder { get; set; }

        /// <summary>
        /// Optional gets or sets the base path for the File/@Source.
        /// </summary>
        /// <remarks>Default value is "SourceDir" to enable use of BindPaths.</remarks>
        string BaseSourcePath { get; set; }

        /// <summary>
        /// Gets or sets the intermediate folder.
        /// </summary>
        string IntermediateFolder { get; set; }

        /// <summary>
        /// Gets or sets where to output the result.
        /// </summary>
        string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress custom tables.
        /// </summary>
        bool SuppressCustomTables { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress dropping empty tables.
        /// </summary>
        bool SuppressDroppingEmptyTables { get; set; }

        /// <summary>
        /// Gets or sets whether to prevent extract cabinets.
        /// </summary>
        bool SuppressExtractCabinets { get; set; }

        /// <summary>
        /// Gets or sets whether to suppress relative action sequencing.
        /// </summary>
        bool SuppressRelativeActionSequencing { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress decompiling UI-related tables.
        /// </summary>
        bool SuppressUI { get; set; }

        /// <summary>
        /// Gets or sets whether the decompiler should keep modularization
        /// GUIDs (true) or remove them (default/false).
        /// </summary>
        bool TreatProductAsModule { get; set; }
    }
}
