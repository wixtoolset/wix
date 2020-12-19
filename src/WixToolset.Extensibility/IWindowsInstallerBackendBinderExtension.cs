// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IWindowsInstallerBackendBinderExtension
    {
#pragma warning disable 1591 // TODO: add documentation
        IEnumerable<TableDefinition> TableDefinitions { get; }
#pragma warning restore 1591

        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBackendBind(IBindContext context);

#pragma warning disable 1591 // TODO: add documentation
        IResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<IBindFileWithPath> files);

        string ResolveMedia(MediaSymbol mediaRow, string mediaLayoutDirectory, string layoutDirectory);

        bool TryAddSymbolToOutput(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData output, TableDefinitionCollection tableDefinitions);
#pragma warning restore 1591

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBackendBind(IBindResult result);
    }
}
