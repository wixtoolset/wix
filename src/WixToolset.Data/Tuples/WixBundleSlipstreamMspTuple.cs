// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleSlipstreamMsp = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleSlipstreamMsp,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.WixBundlePackage_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.WixBundlePackage_Msp), IntermediateFieldType.String),
            },
            typeof(WixBundleSlipstreamMspTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleSlipstreamMspTupleFields
    {
        WixBundlePackage_,
        WixBundlePackage_Msp,
    }

    public class WixBundleSlipstreamMspTuple : IntermediateTuple
    {
        public WixBundleSlipstreamMspTuple() : base(TupleDefinitions.WixBundleSlipstreamMsp, null, null)
        {
        }

        public WixBundleSlipstreamMspTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleSlipstreamMsp, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleSlipstreamMspTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackage_
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.WixBundlePackage_];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.WixBundlePackage_, value);
        }

        public string WixBundlePackage_Msp
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.WixBundlePackage_Msp];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.WixBundlePackage_Msp, value);
        }
    }
}