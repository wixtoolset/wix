// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleMsuPackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleMsuPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsuPackageTupleFields.WixBundlePackage_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsuPackageTupleFields.DetectCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsuPackageTupleFields.MsuKB), IntermediateFieldType.String),
            },
            typeof(WixBundleMsuPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleMsuPackageTupleFields
    {
        WixBundlePackage_,
        DetectCondition,
        MsuKB,
    }

    public class WixBundleMsuPackageTuple : IntermediateTuple
    {
        public WixBundleMsuPackageTuple() : base(TupleDefinitions.WixBundleMsuPackage, null, null)
        {
        }

        public WixBundleMsuPackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleMsuPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsuPackageTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackage_
        {
            get => (string)this.Fields[(int)WixBundleMsuPackageTupleFields.WixBundlePackage_]?.Value;
            set => this.Set((int)WixBundleMsuPackageTupleFields.WixBundlePackage_, value);
        }

        public string DetectCondition
        {
            get => (string)this.Fields[(int)WixBundleMsuPackageTupleFields.DetectCondition]?.Value;
            set => this.Set((int)WixBundleMsuPackageTupleFields.DetectCondition, value);
        }

        public string MsuKB
        {
            get => (string)this.Fields[(int)WixBundleMsuPackageTupleFields.MsuKB]?.Value;
            set => this.Set((int)WixBundleMsuPackageTupleFields.MsuKB, value);
        }
    }
}