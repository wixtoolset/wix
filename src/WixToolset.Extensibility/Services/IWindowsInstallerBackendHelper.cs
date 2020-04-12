// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Interface provided to help Windows Installer backend extensions.
    /// </summary>
    public interface IWindowsInstallerBackendHelper
    {
        Row CreateRow(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinition tableDefinition);
        bool TryAddTupleToOutputMatchingTableDefinitions(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinitionCollection tableDefinitions);
    }
}
