// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using WixToolset.Data;
    using ForTestingUseOnly.Symbols;

    public static partial class ForTestingUseOnlySymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ForTestingUseOnlyBundle = new IntermediateSymbolDefinition(
            ForTestingUseOnlySymbolDefinitionType.ForTestingUseOnlyBundle.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ForTestingUseOnlyBundleSymbolFields.BundleCode), IntermediateFieldType.String),
            },
            typeof(ForTestingUseOnlyBundleSymbol));
    }
}

namespace ForTestingUseOnly.Symbols
{
    using WixToolset.Data;

    public enum ForTestingUseOnlyBundleSymbolFields
    {
        BundleCode,
    }

    public class ForTestingUseOnlyBundleSymbol : IntermediateSymbol
    {
        public ForTestingUseOnlyBundleSymbol() : base(ForTestingUseOnlySymbolDefinitions.ForTestingUseOnlyBundle, null, null)
        {
        }

        public ForTestingUseOnlyBundleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ForTestingUseOnlySymbolDefinitions.ForTestingUseOnlyBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ForTestingUseOnlyBundleSymbolFields index] => this.Fields[(int)index];

        public string BundleCode
        {
            get => this.Fields[(int)ForTestingUseOnlyBundleSymbolFields.BundleCode].AsString();
            set => this.Set((int)ForTestingUseOnlyBundleSymbolFields.BundleCode, value);
        }
    }
}
