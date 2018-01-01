// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Netfx.Tuples;

    /// <summary>
    /// The WiX Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetfxExtensionData : BaseExtensionData
    {
        public override bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = (name == NetfxTupleDefinitionNames.NetFxNativeImage) ? NetfxTupleDefinitions.NetFxNativeImage : null;
            return tupleDefinition != null;
        }

        public override Intermediate GetLibrary(ITupleDefinitionCreator tupleDefinitions)
        {
            return Intermediate.Load(typeof(NetfxExtensionData).Assembly, "WixToolset.Netfx.netfx.wixlib", tupleDefinitions);
        }
    }
}
