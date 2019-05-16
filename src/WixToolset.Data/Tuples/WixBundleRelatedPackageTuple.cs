// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleRelatedPackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleRelatedPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.WixBundlePackage_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.Id), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.Languages), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MinInclusive), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MaxInclusive), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.LangInclusive), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.OnlyDetect), IntermediateFieldType.Number),
            },
            typeof(WixBundleRelatedPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleRelatedPackageTupleFields
    {
        WixBundlePackage_,
        Id,
        MinVersion,
        MaxVersion,
        Languages,
        MinInclusive,
        MaxInclusive,
        LangInclusive,
        OnlyDetect,
    }

    public class WixBundleRelatedPackageTuple : IntermediateTuple
    {
        public WixBundleRelatedPackageTuple() : base(TupleDefinitions.WixBundleRelatedPackage, null, null)
        {
        }

        public WixBundleRelatedPackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleRelatedPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleRelatedPackageTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackage_
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.WixBundlePackage_];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.WixBundlePackage_, value);
        }

        public string Id
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.Id];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.Id, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.MinVersion];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.MaxVersion];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.MaxVersion, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.Languages];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.Languages, value);
        }

        public int MinInclusive
        {
            get => (int)this.Fields[(int)WixBundleRelatedPackageTupleFields.MinInclusive];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.MinInclusive, value);
        }

        public int MaxInclusive
        {
            get => (int)this.Fields[(int)WixBundleRelatedPackageTupleFields.MaxInclusive];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.MaxInclusive, value);
        }

        public int LangInclusive
        {
            get => (int)this.Fields[(int)WixBundleRelatedPackageTupleFields.LangInclusive];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.LangInclusive, value);
        }

        public int OnlyDetect
        {
            get => (int)this.Fields[(int)WixBundleRelatedPackageTupleFields.OnlyDetect];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.OnlyDetect, value);
        }
    }
}