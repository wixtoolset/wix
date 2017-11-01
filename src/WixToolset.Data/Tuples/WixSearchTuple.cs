// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.WixSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSearchTupleFields.WixSearch), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchTupleFields.Variable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixSearchTupleFields
    {
        WixSearch,
        Variable,
        Condition,
    }

    public class WixSearchTuple : IntermediateTuple
    {
        public WixSearchTuple() : base(TupleDefinitions.WixSearch, null, null)
        {
        }

        public WixSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSearchTupleFields index] => this.Fields[(int)index];

        public string WixSearch
        {
            get => (string)this.Fields[(int)WixSearchTupleFields.WixSearch]?.Value;
            set => this.Set((int)WixSearchTupleFields.WixSearch, value);
        }

        public string Variable
        {
            get => (string)this.Fields[(int)WixSearchTupleFields.Variable]?.Value;
            set => this.Set((int)WixSearchTupleFields.Variable, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixSearchTupleFields.Condition]?.Value;
            set => this.Set((int)WixSearchTupleFields.Condition, value);
        }
    }
}