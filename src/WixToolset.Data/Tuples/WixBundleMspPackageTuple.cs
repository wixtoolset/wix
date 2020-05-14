// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleMspPackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleMspPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageTupleFields.PatchCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageTupleFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageTupleFields.PatchXml), IntermediateFieldType.String),
            },
            typeof(WixBundleMspPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleMspPackageTupleFields
    {
        Attributes,
        PatchCode,
        Manufacturer,
        PatchXml,
    }

    [Flags]
    public enum WixBundleMspPackageAttributes
    {
        Slipstream = 0x2,
        TargetUnspecified = 0x4,
    }

    public class WixBundleMspPackageTuple : IntermediateTuple
    {
        public WixBundleMspPackageTuple() : base(TupleDefinitions.WixBundleMspPackage, null, null)
        {
        }

        public WixBundleMspPackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleMspPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMspPackageTupleFields index] => this.Fields[(int)index];

        public WixBundleMspPackageAttributes Attributes
        {
            get => (WixBundleMspPackageAttributes)(int)this.Fields[(int)WixBundleMspPackageTupleFields.Attributes];
            set => this.Set((int)WixBundleMspPackageTupleFields.Attributes, (int)value);
        }

        public string PatchCode
        {
            get => (string)this.Fields[(int)WixBundleMspPackageTupleFields.PatchCode];
            set => this.Set((int)WixBundleMspPackageTupleFields.PatchCode, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleMspPackageTupleFields.Manufacturer];
            set => this.Set((int)WixBundleMspPackageTupleFields.Manufacturer, value);
        }

        public string PatchXml
        {
            get => (string)this.Fields[(int)WixBundleMspPackageTupleFields.PatchXml];
            set => this.Set((int)WixBundleMspPackageTupleFields.PatchXml, value);
        }

        public bool Slipstream => (this.Attributes & WixBundleMspPackageAttributes.Slipstream) == WixBundleMspPackageAttributes.Slipstream;

        public bool TargetUnspecified => (this.Attributes & WixBundleMspPackageAttributes.TargetUnspecified) == WixBundleMspPackageAttributes.TargetUnspecified;
    }
}