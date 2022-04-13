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
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.LogPathVariable), IntermediateFieldType.String),
            },
            typeof(WixBundleRollbackBoundarySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleRollbackBoundarySymbolFields
    {
        Attributes,
        LogPathVariable,
    }

    [Flags]
    public enum WixBundleRollbackBoundaryAttributes
    {
        None = 0x0,
        Vital = 0x1,
        Transaction = 0x2,
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

        public WixBundleRollbackBoundaryAttributes Attributes
        {
            get => (WixBundleRollbackBoundaryAttributes)this.Fields[(int)WixBundleRollbackBoundarySymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleRollbackBoundarySymbolFields.Attributes, (int)value);
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundleRollbackBoundarySymbolFields.LogPathVariable];
            set => this.Set((int)WixBundleRollbackBoundarySymbolFields.LogPathVariable, value);
        }

        public bool Vital
        {
            get { return this.Attributes.HasFlag(WixBundleRollbackBoundaryAttributes.Vital); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRollbackBoundaryAttributes.Vital;
                }
                else
                {
                    this.Attributes &= ~WixBundleRollbackBoundaryAttributes.Vital;
                }
            }
        }

        public bool Transaction
        {
            get { return this.Attributes.HasFlag(WixBundleRollbackBoundaryAttributes.Transaction); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRollbackBoundaryAttributes.Transaction;
                }
                else
                {
                    this.Attributes &= ~WixBundleRollbackBoundaryAttributes.Transaction;
                }
            }
        }
    }
}
