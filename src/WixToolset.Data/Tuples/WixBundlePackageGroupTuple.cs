// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePackageGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePackageGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageGroupTupleFields.WixBundlePackageGroup), IntermediateFieldType.String),
            },
            typeof(WixBundlePackageGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePackageGroupTupleFields
    {
        WixBundlePackageGroup,
    }

    public class WixBundlePackageGroupTuple : IntermediateTuple
    {
        public WixBundlePackageGroupTuple() : base(TupleDefinitions.WixBundlePackageGroup, null, null)
        {
        }

        public WixBundlePackageGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePackageGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageGroupTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackageGroup
        {
            get => (string)this.Fields[(int)WixBundlePackageGroupTupleFields.WixBundlePackageGroup]?.Value;
            set => this.Set((int)WixBundlePackageGroupTupleFields.WixBundlePackageGroup, value);
        }
    }
}