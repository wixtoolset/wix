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
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.RelatedId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.Languages), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleRelatedPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleRelatedPackageTupleFields
    {
        PackageRef,
        RelatedId,
        MinVersion,
        MaxVersion,
        Languages,
        Attributes,
    }

    [Flags]
    public enum WixBundleRelatedPackageAttributes
    {
        None = 0x0,
        OnlyDetect = 0x1,
        MinInclusive = 0x2,
        MaxInclusive = 0x4,
        LangInclusive = 0x8,
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

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.PackageRef];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.PackageRef, value);
        }

        public string RelatedId
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageTupleFields.RelatedId];
            set => this.Set((int)WixBundleRelatedPackageTupleFields.RelatedId, value);
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

        public WixBundleRelatedPackageAttributes Attributes
        {
            get => (WixBundleRelatedPackageAttributes)this.Fields[(int)WixBundleRelatedPackageTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleRelatedPackageTupleFields.Attributes, (int)value);
        }

        public bool MinInclusive => (this.Attributes & WixBundleRelatedPackageAttributes.MinInclusive) == WixBundleRelatedPackageAttributes.MinInclusive;

        public bool MaxInclusive => (this.Attributes & WixBundleRelatedPackageAttributes.MaxInclusive) == WixBundleRelatedPackageAttributes.MaxInclusive;

        public bool OnlyDetect => (this.Attributes & WixBundleRelatedPackageAttributes.OnlyDetect) == WixBundleRelatedPackageAttributes.OnlyDetect;

        public bool LangInclusive => (this.Attributes & WixBundleRelatedPackageAttributes.LangInclusive) == WixBundleRelatedPackageAttributes.LangInclusive;
    }
}
