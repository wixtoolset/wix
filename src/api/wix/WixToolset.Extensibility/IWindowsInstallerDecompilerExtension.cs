// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Interface all windows installer decompiler extensions implement.
    /// </summary>
    public interface IWindowsInstallerDecompilerExtension
    {
        /// <summary>
        /// Gets the table definitions this extension decompiles.
        /// </summary>
        /// <value>Table definitions this extension decompiles.</value>
        IReadOnlyCollection<TableDefinition> TableDefinitions { get; }

        /// <summary>
        /// Called before decompiling occurs.
        /// </summary>
        /// <param name="context">Decompile context.</param>
        /// <param name="helper">Decompile helper.</param>
        void PreDecompile(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper helper);

        /// <summary>
        /// Called before decompiling occurs.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        void PreDecompileTables(TableIndexedCollection tables);

        /// <summary>
        /// Try to decompile an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        /// <returns>True if the table was decompiled, false otherwise.</returns>
        bool TryDecompileTable(Table table);

        /// <summary>
        /// After decompilation tables.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        void PostDecompileTables(TableIndexedCollection tables);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostDecompile(IWindowsInstallerDecompileResult result);
    }
}
