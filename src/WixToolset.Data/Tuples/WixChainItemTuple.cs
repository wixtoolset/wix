// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixChainItem = new IntermediateTupleDefinition(
            TupleDefinitionType.WixChainItem,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixChainItemTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixChainItemTupleFields
    {
    }

    public class WixChainItemTuple : IntermediateTuple
    {
        public WixChainItemTuple() : base(TupleDefinitions.WixChainItem, null, null)
        {
        }

        public WixChainItemTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixChainItem, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixChainItemTupleFields index] => this.Fields[(int)index];
    }
}