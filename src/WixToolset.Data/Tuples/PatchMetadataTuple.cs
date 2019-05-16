// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PatchMetadata = new IntermediateTupleDefinition(
            TupleDefinitionType.PatchMetadata,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchMetadataTupleFields.Company), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchMetadataTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchMetadataTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(PatchMetadataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PatchMetadataTupleFields
    {
        Company,
        Property,
        Value,
    }

    public class PatchMetadataTuple : IntermediateTuple
    {
        public PatchMetadataTuple() : base(TupleDefinitions.PatchMetadata, null, null)
        {
        }

        public PatchMetadataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.PatchMetadata, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchMetadataTupleFields index] => this.Fields[(int)index];

        public string Company
        {
            get => (string)this.Fields[(int)PatchMetadataTupleFields.Company];
            set => this.Set((int)PatchMetadataTupleFields.Company, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)PatchMetadataTupleFields.Property];
            set => this.Set((int)PatchMetadataTupleFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)PatchMetadataTupleFields.Value];
            set => this.Set((int)PatchMetadataTupleFields.Value, value);
        }
    }
}