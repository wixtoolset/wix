// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Netfx.Symbols;

    public static partial class NetfxSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition NetFxNetCoreSdkSearch = new IntermediateSymbolDefinition(
            NetfxSymbolDefinitionType.NetFxNetCoreSdkSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkSearchSymbolFields.Platform), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(NetFxNetCoreSdkSearchSymbolFields.Version), IntermediateFieldType.String),
            },
            typeof(NetFxNetCoreSearchSymbol));
    }
}

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;


    public enum NetCoreSdkSearchPlatform
    {
        X86,
        X64,
        Arm64,
    }

    public enum NetFxNetCoreSdkSearchSymbolFields
    {
        Platform,
        Version,
    }


    public class NetFxNetCoreSdkSearchSymbol : IntermediateSymbol
    {
        public NetFxNetCoreSdkSearchSymbol() : base(NetfxSymbolDefinitions.NetFxNetCoreSdkSearch, null, null)
        {
        }

        public NetFxNetCoreSdkSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxNetCoreSdkSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNetCoreSdkSearchSymbolFields index] => this.Fields[(int)index];

        public NetCoreSdkSearchPlatform Platform
        {
            get => (NetCoreSdkSearchPlatform)this.Fields[(int)NetFxNetCoreSdkSearchSymbolFields.Platform].AsNumber();
            set => this.Set((int)NetFxNetCoreSdkSearchSymbolFields.Platform, (int)value);
        }

        public string Version
        {
            get => this.Fields[(int)NetFxNetCoreSdkSearchSymbolFields.Version].AsString();
            set => this.Set((int)NetFxNetCoreSdkSearchSymbolFields.Version, value);
        }
    }
}
