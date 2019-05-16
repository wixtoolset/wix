// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFragment = new IntermediateTupleDefinition(
            TupleDefinitionType.WixFragment,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFragmentTupleFields.WixFragment), IntermediateFieldType.String),
            },
            typeof(WixFragmentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixFragmentTupleFields
    {
        WixFragment,
    }

    public class WixFragmentTuple : IntermediateTuple
    {
        public WixFragmentTuple() : base(TupleDefinitions.WixFragment, null, null)
        {
        }

        public WixFragmentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixFragment, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFragmentTupleFields index] => this.Fields[(int)index];

        public string WixFragment
        {
            get => (string)this.Fields[(int)WixFragmentTupleFields.WixFragment];
            set => this.Set((int)WixFragmentTupleFields.WixFragment, value);
        }
    }
}