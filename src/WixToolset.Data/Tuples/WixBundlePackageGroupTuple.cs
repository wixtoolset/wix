// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePackageGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePackageGroup,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundlePackageGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePackageGroupTupleFields
    {
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
    }
}