// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset Dependency Extension.
    /// </summary>
    public sealed class DependencyExtensionData : BaseExtensionData
    {
        /// <summary>
        /// Gets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        public override string DefaultCulture => "en-US";

        /// <summary>
        /// Gets the contained .wixlib content.
        /// </summary>
        /// <param name="symbolDefinitions">Strong typed symbold definitions.</param>
        /// <returns>The .wixlib.</returns>
        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(DependencyExtensionData).Assembly, "WixToolset.Dependency.dependency.wixlib", symbolDefinitions);
        }
    }
}
