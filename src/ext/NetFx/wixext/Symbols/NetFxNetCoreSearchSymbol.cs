// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Netfx.Symbols;

    public static partial class NetfxSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition NetFxNetCoreSearch = new IntermediateSymbolDefinition(
            NetfxSymbolDefinitionType.NetFxNetCoreSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSearchSymbolFields.RuntimeType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSearchSymbolFields.Platform), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSearchSymbolFields.MajorVersion), IntermediateFieldType.Number),
            },
            typeof(NetFxNetCoreSearchSymbol));
    }
}

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public enum NetCoreSearchRuntimeType
    {
        Core,
        Aspnet,
        Desktop,
    }

    public enum NetCoreSearchPlatform
    {
        X86,
        X64,
        Arm64,
    }

    public enum NetFxNetCoreSearchSymbolFields
    {
        RuntimeType,
        Platform,
        MajorVersion,
    }

    public class NetFxNetCoreSearchSymbol : IntermediateSymbol
    {
        public NetFxNetCoreSearchSymbol() : base(NetfxSymbolDefinitions.NetFxNetCoreSearch, null, null)
        {
        }

        public NetFxNetCoreSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxNetCoreSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNetCoreSearchSymbolFields index] => this.Fields[(int)index];

        public NetCoreSearchRuntimeType RuntimeType
        {
            get => (NetCoreSearchRuntimeType)this.Fields[(int)NetFxNetCoreSearchSymbolFields.RuntimeType].AsNumber();
            set => this.Set((int)NetFxNetCoreSearchSymbolFields.RuntimeType, (int)value);
        }

        public NetCoreSearchPlatform Platform
        {
            get => (NetCoreSearchPlatform)this.Fields[(int)NetFxNetCoreSearchSymbolFields.Platform].AsNumber();
            set => this.Set((int)NetFxNetCoreSearchSymbolFields.Platform, (int)value);
        }

        public int MajorVersion
        {
            get => this.Fields[(int)NetFxNetCoreSearchSymbolFields.MajorVersion].AsNumber();
            set => this.Set((int)NetFxNetCoreSearchSymbolFields.MajorVersion, value);
        }
    }
}
