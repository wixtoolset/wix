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
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.TargetPackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSlipstreamMspTupleFields.MspPackageRef), IntermediateFieldType.String),
            },
            typeof(WixBundleSlipstreamMspTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleSlipstreamMspTupleFields
    {
        TargetPackageRef,
        MspPackageRef,
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

        public string TargetPackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.TargetPackageRef];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.TargetPackageRef, value);
        }

        public string MspPackageRef
        {
            get => (string)this.Fields[(int)WixBundleSlipstreamMspTupleFields.MspPackageRef];
            set => this.Set((int)WixBundleSlipstreamMspTupleFields.MspPackageRef, value);
        }
    }
}
