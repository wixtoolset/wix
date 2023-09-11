// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum NetfxSymbolDefinitionType
    {
        NetFxNativeImage,
        NetFxNetCoreSearch,
        NetFxDotNetCompatibilityCheck,
        NetFxNetCoreSdkSearch,
        NetFxNetCoreSdkFeatureBandSearch,
    }

    public static partial class NetfxSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out NetfxSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(NetfxSymbolDefinitionType type)
        {
            switch (type)
            {
                case NetfxSymbolDefinitionType.NetFxNativeImage:
                    return NetfxSymbolDefinitions.NetFxNativeImage;

                case NetfxSymbolDefinitionType.NetFxNetCoreSearch:
                    return NetfxSymbolDefinitions.NetFxNetCoreSearch;

                case NetfxSymbolDefinitionType.NetFxNetCoreSdkSearch:
                    return NetfxSymbolDefinitions.NetFxNetCoreSdkSearch;

                case NetfxSymbolDefinitionType.NetFxNetCoreSdkFeatureBandSearch:
                    return NetfxSymbolDefinitions.NetFxNetCoreSdkFeatureBandSearch;

                case NetfxSymbolDefinitionType.NetFxDotNetCompatibilityCheck:
                    return NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static NetfxSymbolDefinitions()
        {
            NetFxNetCoreSearch.AddTag(BurnConstants.BundleExtensionSearchSymbolDefinitionTag);
            NetFxNetCoreSdkSearch.AddTag(BurnConstants.BundleExtensionSearchSymbolDefinitionTag);
            NetFxNetCoreSdkFeatureBandSearch.AddTag(BurnConstants.BundleExtensionSearchSymbolDefinitionTag);
        }
    }
}
