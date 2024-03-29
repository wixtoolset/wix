// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to help Windows Installer backend extensions.
    /// </summary>
    public interface IWindowsInstallerBackendHelper : IBackendHelper
    {
        /// <summary>
        /// Creates a file facade from a <c>FileSymbol</c>.
        /// </summary>
        /// <param name="file"><c>FileSymbol</c> backing the facade.</param>
        /// <returns></returns>
        IFileFacade CreateFileFacade(FileSymbol file);

        /// <summary>
        /// Creates a file facade from a File row.
        /// </summary>
        /// <param name="fileRow"><c>FileRow</c></param>
        /// <returns>New <c>IFileFacade</c>.</returns>
        IFileFacade CreateFileFacade(FileRow fileRow);

        /// <summary>
        /// Creates a <see cref="Row"/> in the specified table.
        /// </summary>
        /// <param name="section">Parent section.</param>
        /// <param name="symbol">Symbol with line information for the row.</param>
        /// <param name="data">Windows Installer data.</param>
        /// <param name="tableDefinition">Table definition for the row.</param>
        /// <returns>Row created in the <paramref name="data"/>.</returns>
        Row CreateRow(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData data, TableDefinition tableDefinition);

        /// <summary>
        /// Looks up the registered <see cref="TableDefinition"/> for the given <see cref="IntermediateSymbol"/> and creates a <see cref="Row"/> in that table.
        /// Goes sequentially through each field in the symbol and assigns the value to the column with the same index as the field.
        /// If the symbol's Id is registered as the primary key then that is used for the first column and the column data is offset by 1.
        /// </summary>
        /// <param name="section">Parent section.</param>
        /// <param name="symbol">Symbol to create the row from.</param>
        /// <param name="data">Windows Installer data.</param>
        /// <param name="tableDefinitions">Table definitions that have been registered with the binder.</param>
        /// <returns>True if a row was created.</returns>
        bool TryAddSymbolToMatchingTableDefinitions(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData data, TableDefinitionCollection tableDefinitions);
    }
}
