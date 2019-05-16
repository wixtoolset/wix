// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePayloadGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePayloadGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePayloadGroupTupleFields.WixBundlePayloadGroup), IntermediateFieldType.String),
            },
            typeof(WixBundlePayloadGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePayloadGroupTupleFields
    {
        WixBundlePayloadGroup,
    }

    public class WixBundlePayloadGroupTuple : IntermediateTuple
    {
        public WixBundlePayloadGroupTuple() : base(TupleDefinitions.WixBundlePayloadGroup, null, null)
        {
        }

        public WixBundlePayloadGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePayloadGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePayloadGroupTupleFields index] => this.Fields[(int)index];

        public string WixBundlePayloadGroup
        {
            get => (string)this.Fields[(int)WixBundlePayloadGroupTupleFields.WixBundlePayloadGroup];
            set => this.Set((int)WixBundlePayloadGroupTupleFields.WixBundlePayloadGroup, value);
        }
    }
}