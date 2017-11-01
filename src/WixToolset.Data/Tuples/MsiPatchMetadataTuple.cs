// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchMetadata = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchMetadata,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataTupleFields.Company), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiPatchMetadataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchMetadataTupleFields
    {
        Company,
        Property,
        Value,
    }

    public class MsiPatchMetadataTuple : IntermediateTuple
    {
        public MsiPatchMetadataTuple() : base(TupleDefinitions.MsiPatchMetadata, null, null)
        {
        }

        public MsiPatchMetadataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchMetadata, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchMetadataTupleFields index] => this.Fields[(int)index];

        public string Company
        {
            get => (string)this.Fields[(int)MsiPatchMetadataTupleFields.Company]?.Value;
            set => this.Set((int)MsiPatchMetadataTupleFields.Company, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)MsiPatchMetadataTupleFields.Property]?.Value;
            set => this.Set((int)MsiPatchMetadataTupleFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiPatchMetadataTupleFields.Value]?.Value;
            set => this.Set((int)MsiPatchMetadataTupleFields.Value, value);
        }
    }
}