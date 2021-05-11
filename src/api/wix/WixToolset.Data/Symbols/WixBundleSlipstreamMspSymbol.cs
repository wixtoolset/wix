// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleSlipstreamMsp = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleSlipstreamMsp,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspSymbolFields.TargetPackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspSymbolFields.MspPackageRef), IntermediateFieldType.String),
            },
            typeof(WixBundleSlipstreamMspSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleSlipstreamMspSymbolFields
    {
        TargetPackageRef,
        MspPackageRef,
    }

    public class WixBundleSlipstreamMspSymbol : IntermediateSymbol
    {
        public WixBundleSlipstreamMspSymbol() : base(SymbolDefinitions.WixBundleSlipstreamMsp, null, null)
        {
        }

        public WixBundleSlipstreamMspSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleSlipstreamMsp, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleSlipstreamMspSymbolFields index] => this.Fields[(int)index];

        public string TargetPackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspSymbolFields.TargetPackageRef];
            set => this.Set((int)WixBundleSlipstreamMspSymbolFields.TargetPackageRef, value);
        }

        public string MspPackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspSymbolFields.MspPackageRef];
            set => this.Set((int)WixBundleSlipstreamMspSymbolFields.MspPackageRef, value);
        }
    }
}
