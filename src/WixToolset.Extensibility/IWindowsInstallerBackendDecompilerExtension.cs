// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IWindowsInstallerBackendDecompilerExtension
    {
        /// <summary>
        /// Called before decompiling occurs.
        /// </summary>
        void PreBackendDecompile(IDecompileContext context);

        /// <summary>
        /// Gets the table definitions this extension decompiles.
        /// </summary>
        /// <value>Table definitions this extension decompiles.</value>
        TableDefinitionCollection TableDefinitions { get; }

        /// <summary>
        /// Gets the library that this decompiler wants removed from the decomipiled output.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The library for this extension or null if there is no library to be removed.</returns>
        Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions);

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        void DecompileTable(Table table);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBackendDecompile(DecompileResult result);
    }
}
