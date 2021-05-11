// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsExtensionData : BaseExtensionData
    {
        /// <summary>
        /// Gets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        public override string DefaultCulture => "en-US";

        public override bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = IisSymbolDefinitions.ByName(name);
            return symbolDefinition != null;
        }

        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(IIsExtensionData).Assembly, "WixToolset.Iis.iis.wixlib", symbolDefinitions);
        }
    }
}
