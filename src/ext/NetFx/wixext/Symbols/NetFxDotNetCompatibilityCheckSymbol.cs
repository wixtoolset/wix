// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data;
    using WixToolset.Netfx.Symbols;

    public static partial class NetfxSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition NetFxDotNetCompatibilityCheck = new IntermediateSymbolDefinition(
            NetfxSymbolDefinitionType.NetFxDotNetCompatibilityCheck.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbolFields.RuntimeType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbolFields.Platform), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbolFields.RollForward), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbolFields.Property), IntermediateFieldType.String),
            },
            typeof(NetFxDotNetCompatibilityCheckSymbol));
    }
}

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public enum NetFxDotNetCompatibilityCheckSymbolFields
    {
        RuntimeType,
        Platform,
        Version,
        RollForward,
        Property,
    }

    public class NetFxDotNetCompatibilityCheckSymbol : IntermediateSymbol
    {
        public NetFxDotNetCompatibilityCheckSymbol() : base(NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck, null, null)
        {
        }

        public NetFxDotNetCompatibilityCheckSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxDotNetCompatibilityCheckSymbolFields index] => this.Fields[(int)index];

        public string RuntimeType
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbolFields.RuntimeType].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbolFields.RuntimeType, value);
        }

        public string Platform
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbolFields.Platform].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbolFields.Platform, value);
        }

        public string Version
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbolFields.Version].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbolFields.Version, value);
        }

        public string RollForward
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbolFields.RollForward].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbolFields.RollForward, value);
        }

        public string Property
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbolFields.Property].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbolFields.Property, value);
        }
    }
}
