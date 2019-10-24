// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx.Tuples
{
    using WixToolset.Data;

    public static class NetfxTupleDefinitionNames
    {
        public static string NetFxNativeImage { get; } = "NetFxNativeImage";
    }

    public static class NetfxTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition NetFxNativeImage = new IntermediateTupleDefinition(
            NetfxTupleDefinitionNames.NetFxNativeImage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxNativeImageTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageTupleFields.Priority), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageTupleFields.ApplicationFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxNativeImageTupleFields.ApplicationBaseDirectoryRef), IntermediateFieldType.String),
            },
            typeof(NetFxNativeImageTuple));
    }
}
