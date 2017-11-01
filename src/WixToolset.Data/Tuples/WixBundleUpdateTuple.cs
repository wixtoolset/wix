// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleUpdate = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleUpdate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleUpdateTupleFields.Location), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleUpdateTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleUpdateTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleUpdateTupleFields
    {
        Location,
        Attributes,
    }

    public class WixBundleUpdateTuple : IntermediateTuple
    {
        public WixBundleUpdateTuple() : base(TupleDefinitions.WixBundleUpdate, null, null)
        {
        }

        public WixBundleUpdateTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleUpdate,sourceLineNumber,id)
        {
        }

        public IntermediateField this[WixBundleUpdateTupleFields index] => this.Fields[(int)index];

        public string Location
        {
            get => (string)this.Fields[(int)WixBundleUpdateTupleFields.Location]?.Value;
            set => this.Set((int)WixBundleUpdateTupleFields.Location, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixBundleUpdateTupleFields.Attributes]?.Value;
            set => this.Set((int)WixBundleUpdateTupleFields.Attributes, value);
        }
    }
}