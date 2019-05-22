// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixProductSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.WixProductSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixProductSearchTupleFields.WixSearchRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductSearchTupleFields.Guid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductSearchTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixProductSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixProductSearchTupleFields
    {
        WixSearchRef,
        Guid,
        Attributes,
    }

    public class WixProductSearchTuple : IntermediateTuple
    {
        public WixProductSearchTuple() : base(TupleDefinitions.WixProductSearch, null, null)
        {
        }

        public WixProductSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixProductSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixProductSearchTupleFields index] => this.Fields[(int)index];

        public string WixSearchRef
        {
            get => (string)this.Fields[(int)WixProductSearchTupleFields.WixSearchRef];
            set => this.Set((int)WixProductSearchTupleFields.WixSearchRef, value);
        }

        public string Guid
        {
            get => (string)this.Fields[(int)WixProductSearchTupleFields.Guid];
            set => this.Set((int)WixProductSearchTupleFields.Guid, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixProductSearchTupleFields.Attributes];
            set => this.Set((int)WixProductSearchTupleFields.Attributes, value);
        }
    }
}