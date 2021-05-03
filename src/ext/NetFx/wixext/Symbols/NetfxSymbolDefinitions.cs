// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public static class NetfxSymbolDefinitionNames
    {
        public static string NetFxNativeImage { get; } = "NetFxNativeImage";
    }

    public static class NetfxSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition NetFxNativeImage = new IntermediateSymbolDefinition(
            NetfxSymbolDefinitionNames.NetFxNativeImage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxNativeImageSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageSymbolFields.Priority), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageSymbolFields.ApplicationFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageSymbolFields.ApplicationBaseDirectoryRef), IntermediateFieldType.String),
            },
            typeof(NetFxNativeImageSymbol));
    }
}
