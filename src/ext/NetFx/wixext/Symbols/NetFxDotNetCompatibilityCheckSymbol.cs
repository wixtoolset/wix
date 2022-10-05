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
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbollFields.RuntimeType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbollFields.Platform), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbollFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbollFields.RollForward), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(NetFxDotNetCompatibilityCheckSymbollFields.Variable), IntermediateFieldType.String),
            },
            typeof(NetFxNativeImageSymbol));
    }
}

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public enum NetFxDotNetCompatibilityCheckSymbollFields
    {
        RuntimeType,
        Platform,
        Version,
        RollForward,
        Variable,
    }

    public class NetFxDotNetCompatibilityCheckSymbol : IntermediateSymbol
    {
        public NetFxDotNetCompatibilityCheckSymbol() : base(NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck, null, null)
        {
        }

        public NetFxDotNetCompatibilityCheckSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxDotNetCompatibilityCheckSymbollFields index] => this.Fields[(int)index];

        public string RuntimeType
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbollFields.RuntimeType].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbollFields.RuntimeType, value);
        }

        public string Platform
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbollFields.Platform].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbollFields.Platform, value);
        }

        public string Version
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbollFields.Version].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbollFields.Version, value);
        }

        public string RollForward
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbollFields.RollForward].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbollFields.RollForward, value);
        }

        public string Variable
        {
            get => this.Fields[(int)NetFxDotNetCompatibilityCheckSymbollFields.Variable].AsString();
            set => this.Set((int)NetFxDotNetCompatibilityCheckSymbollFields.Variable, value);
        }
    }
}
