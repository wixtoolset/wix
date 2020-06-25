// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Interface extensions implement to provide data.
    /// </summary>
    public interface IExtensionData
    {
        /// <summary>
        /// Gets the optional default culture.
        /// </summary>
        /// <value>The optional default culture.</value>
        string DefaultCulture { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="symbolDefinition"></param>
        /// <returns>True </returns>
        bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition);

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="symbolDefinitions">The symbol definitions to use while loading the library.</param>
        /// <returns>The library for this extension or null if there is no library.</returns>
        Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions);
    }
}
