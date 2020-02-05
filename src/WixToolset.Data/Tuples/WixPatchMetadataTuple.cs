// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

// TODO: delete this

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchMetadata = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchMetadata,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchMetadataTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(WixPatchMetadataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchMetadataTupleFields
    {
        Value,
    }

    public class WixPatchMetadataTuple : IntermediateTuple
    {
        public WixPatchMetadataTuple() : base(TupleDefinitions.WixPatchMetadata, null, null)
        {
        }

        public WixPatchMetadataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchMetadata, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchMetadataTupleFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)WixPatchMetadataTupleFields.Value];
            set => this.Set((int)WixPatchMetadataTupleFields.Value, value);
        }
    }
}