// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixChain = new IntermediateTupleDefinition(
            TupleDefinitionType.WixChain,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixChainTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixChainTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixChainTupleFields
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

    public class WixChainTuple : IntermediateTuple
    {
        public WixChainTuple() : base(TupleDefinitions.WixChain, null, null)
        {
        }

        public WixChainTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixChain, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixChainTupleFields index] => this.Fields[(int)index];

        public WixChainAttributes Attributes
        {
            get => (WixChainAttributes)(int)this.Fields[(int)WixChainTupleFields.Attributes]?.Value;
            set => this.Set((int)WixChainTupleFields.Attributes, (int)value);
        }
    }
}