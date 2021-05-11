// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleCustomData = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleCustomData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataSymbolFields.AttributeNames), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataSymbolFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataSymbolFields.BundleExtensionRef), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleCustomDataSymbolFields
    {
        AttributeNames,
        Type,
        BundleExtensionRef,
    }

    public enum WixBundleCustomDataType
    {
        Unknown,
        BootstrapperApplication,
        BundleExtension,
    }

    public class WixBundleCustomDataSymbol : IntermediateSymbol
    {
        public const char AttributeNamesSeparator = '\x85';

        public WixBundleCustomDataSymbol() : base(SymbolDefinitions.WixBundleCustomData, null, null)
        {
        }

        public WixBundleCustomDataSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleCustomData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataSymbolFields index] => this.Fields[(int)index];

        public string AttributeNames
        {
            get => (string)this.Fields[(int)WixBundleCustomDataSymbolFields.AttributeNames];
            set => this.Set((int)WixBundleCustomDataSymbolFields.AttributeNames, value);
        }

        public WixBundleCustomDataType Type
        {
            get => (WixBundleCustomDataType)this.Fields[(int)WixBundleCustomDataSymbolFields.Type].AsNumber();
            set => this.Set((int)WixBundleCustomDataSymbolFields.Type, (int)value);
        }

        public string BundleExtensionRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataSymbolFields.BundleExtensionRef];
            set => this.Set((int)WixBundleCustomDataSymbolFields.BundleExtensionRef, value);
        }

        public string[] AttributeNamesSeparated => this.AttributeNames.Split(AttributeNamesSeparator);
    }
}
