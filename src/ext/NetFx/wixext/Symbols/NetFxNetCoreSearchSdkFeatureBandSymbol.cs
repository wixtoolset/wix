// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Netfx.Symbols;

    public static partial class NetfxSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition NetFxNetCoreSdkFeatureBandSearch = new IntermediateSymbolDefinition(
            NetfxSymbolDefinitionType.NetFxNetCoreSdkFeatureBandSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkFeatureBandSearchSymbolFields.Platform), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkFeatureBandSearchSymbolFields.MajorVersion), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkFeatureBandSearchSymbolFields.MinorVersion), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkFeatureBandSearchSymbolFields.PatchVersion), IntermediateFieldType.Number),
            },
            typeof(NetFxNetCoreSdkFeatureBandSearchSymbol));
    }
}

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public enum NetFxNetCoreSdkFeatureBandSearchSymbolFields
    {
        Platform,
        MajorVersion,
        MinorVersion,
        PatchVersion,
    }

    public class NetFxNetCoreSdkFeatureBandSearchSymbol : IntermediateSymbol
    {
        public NetFxNetCoreSdkFeatureBandSearchSymbol() : base(NetfxSymbolDefinitions.NetFxNetCoreSdkFeatureBandSearch, null, null)
        {
        }

        public NetFxNetCoreSdkFeatureBandSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxNetCoreSdkFeatureBandSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNetCoreSdkFeatureBandSearchSymbolFields index] => this.Fields[(int)index];

        public NetCoreSearchPlatform Platform
        {
            get => (NetCoreSearchPlatform)this.Fields[(int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.Platform].AsNumber();
            set => this.Set((int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.Platform, (int)value);
        }

        public int MajorVersion
        {
            get => this.Fields[(int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.MajorVersion].AsNumber();
            set => this.Set((int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.MajorVersion, value);
        }

        public int MinorVersion
        {
            get => this.Fields[(int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.MinorVersion].AsNumber();
            set => this.Set((int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.MinorVersion, value);
        }

        public int PatchVersion
        {
            get => this.Fields[(int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.PatchVersion].AsNumber();
            set => this.Set((int)NetFxNetCoreSdkFeatureBandSearchSymbolFields.PatchVersion, value);
        }
    }
}
