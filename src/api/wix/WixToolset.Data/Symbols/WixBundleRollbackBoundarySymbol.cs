// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleRollbackBoundary = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleRollbackBoundary,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleRollbackBoundarySymbolFields.Vital), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleRollbackBoundarySymbolFields.Transaction), IntermediateFieldType.Number),
            },
            typeof(WixBundleRollbackBoundarySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleRollbackBoundarySymbolFields
    {
        Vital,
        Transaction,
    }

    public class WixBundleRollbackBoundarySymbol : IntermediateSymbol
    {
        public WixBundleRollbackBoundarySymbol() : base(SymbolDefinitions.WixBundleRollbackBoundary, null, null)
        {
        }

        public WixBundleRollbackBoundarySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleRollbackBoundary, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleRollbackBoundarySymbolFields index] => this.Fields[(int)index];

        public bool? Vital
        {
            get => (bool?)this.Fields[(int)WixBundleRollbackBoundarySymbolFields.Vital];
            set => this.Set((int)WixBundleRollbackBoundarySymbolFields.Vital, value);
        }

        public bool? Transaction
        {
            get => (bool?)this.Fields[(int)WixBundleRollbackBoundarySymbolFields.Transaction];
            set => this.Set((int)WixBundleRollbackBoundarySymbolFields.Transaction, value);
        }
    }
}