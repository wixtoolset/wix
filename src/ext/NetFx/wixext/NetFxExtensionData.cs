// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Netfx.Symbols;

    /// <summary>
    /// The WiX Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetfxExtensionData : BaseExtensionData
    {
        public override bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = (name == NetfxSymbolDefinitionNames.NetFxNativeImage) ? NetfxSymbolDefinitions.NetFxNativeImage : null;
            return symbolDefinition != null;
        }

        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(NetfxExtensionData).Assembly, "WixToolset.Netfx.netfx.wixlib", symbolDefinitions);
        }
    }
}
