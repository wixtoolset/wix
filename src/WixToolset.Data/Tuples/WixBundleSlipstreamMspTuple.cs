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
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.WixBundlePackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.MspWixBundlePackageRef), IntermediateFieldType.String),
            },
            typeof(WixBundleSlipstreamMspTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleSlipstreamMspTupleFields
    {
        WixBundlePackageRef,
        MspWixBundlePackageRef,
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

        public string WixBundlePackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.WixBundlePackageRef];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.WixBundlePackageRef, value);
        }

        public string MspWixBundlePackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.MspWixBundlePackageRef];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.MspWixBundlePackageRef, value);
        }
    }
}