// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixApprovedExeForElevation = new IntermediateTupleDefinition(
            TupleDefinitionType.WixApprovedExeForElevation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Id), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixApprovedExeForElevationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixApprovedExeForElevationTupleFields
    {
        Id,
        Key,
        Value,
        Attributes,
    }

    public class WixApprovedExeForElevationTuple : IntermediateTuple
    {
        public WixApprovedExeForElevationTuple() : base(TupleDefinitions.WixApprovedExeForElevation, null, null)
        {
        }

        public WixApprovedExeForElevationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixApprovedExeForElevation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixApprovedExeForElevationTupleFields index] => this.Fields[(int)index];

        public string Id
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationTupleFields.Id]?.Value;
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Id, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationTupleFields.Key]?.Value;
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationTupleFields.Value]?.Value;
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Value, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixApprovedExeForElevationTupleFields.Attributes]?.Value;
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Attributes, value);
        }
    }
}