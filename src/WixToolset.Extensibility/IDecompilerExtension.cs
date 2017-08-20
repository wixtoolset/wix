// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating a decompiler extension.
    /// </summary>
    public interface IDecompilerExtension
    {
        /// <summary>
        /// Gets or sets the decompiler core for the extension.
        /// </summary>
        /// <value>The decompiler core for the extension.</value>
        IDecompilerCore Core { get; set; }

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
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        void Initialize(TableIndexedCollection tables);

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        void DecompileTable(Table table);

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        void Finish(TableIndexedCollection tables);
    }
}
