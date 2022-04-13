// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixChain = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixChain,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixChainSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixChainSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixChainSymbolFields
    {
        Attributes,
    }

    [Flags]
    public enum WixChainAttributes
    {
        None = 0x0,
        DisableRollback = 0x1,
        DisableSystemRestore = 0x2,
        ParallelCache = 0x4,
    }

    public class WixChainSymbol : IntermediateSymbol
    {
        public WixChainSymbol() : base(SymbolDefinitions.WixChain, null, null)
        {
        }

        public WixChainSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixChain, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixChainSymbolFields index] => this.Fields[(int)index];

        public WixChainAttributes Attributes
        {
            get => (WixChainAttributes)(int)this.Fields[(int)WixChainSymbolFields.Attributes];
            set => this.Set((int)WixChainSymbolFields.Attributes, (int)value);
        }

        public bool DisableRollback
        {
            get { return this.Attributes.HasFlag(WixChainAttributes.DisableRollback); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixChainAttributes.DisableRollback;
                }
                else
                {
                    this.Attributes &= ~WixChainAttributes.DisableRollback;
                }
            }
        }

        public bool DisableSystemRestore
        {
            get { return this.Attributes.HasFlag(WixChainAttributes.DisableSystemRestore); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixChainAttributes.DisableSystemRestore;
                }
                else
                {
                    this.Attributes &= ~WixChainAttributes.DisableSystemRestore;
                }
            }
        }

        public bool ParallelCache
        {
            get { return this.Attributes.HasFlag(WixChainAttributes.ParallelCache); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixChainAttributes.ParallelCache;
                }
                else
                {
                    this.Attributes &= ~WixChainAttributes.ParallelCache;
                }
            }
        }
    }
}
