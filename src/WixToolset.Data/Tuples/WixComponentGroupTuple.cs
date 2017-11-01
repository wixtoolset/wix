// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixComponentGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixComponentGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComponentGroupTupleFields.WixComponentGroup), IntermediateFieldType.String),
            },
            typeof(WixComponentGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixComponentGroupTupleFields
    {
        WixComponentGroup,
    }

    public class WixComponentGroupTuple : IntermediateTuple
    {
        public WixComponentGroupTuple() : base(TupleDefinitions.WixComponentGroup, null, null)
        {
        }

        public WixComponentGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixComponentGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComponentGroupTupleFields index] => this.Fields[(int)index];

        public string WixComponentGroup
        {
            get => (string)this.Fields[(int)WixComponentGroupTupleFields.WixComponentGroup]?.Value;
            set => this.Set((int)WixComponentGroupTupleFields.WixComponentGroup, value);
        }
    }
}