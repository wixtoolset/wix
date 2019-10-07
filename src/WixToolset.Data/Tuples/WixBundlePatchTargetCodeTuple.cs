// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePatchTargetCode = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePatchTargetCode,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.TargetCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundlePatchTargetCodeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundlePatchTargetCodeTupleFields
    {
        PackageRef,
        TargetCode,
        Attributes,
    }

    [Flags]
    public enum WixBundlePatchTargetCodeAttributes : int
    {
        None = 0,

        /// <summary>
        /// The transform targets a specific ProductCode.
        /// </summary>
        TargetsProductCode = 1,

        /// <summary>
        /// The transform targets a specific UpgradeCode.
        /// </summary>
        TargetsUpgradeCode = 2,
    }

    public class WixBundlePatchTargetCodeTuple : IntermediateTuple
    {
        public WixBundlePatchTargetCodeTuple() : base(TupleDefinitions.WixBundlePatchTargetCode, null, null)
        {
        }

        public WixBundlePatchTargetCodeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePatchTargetCode, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePatchTargetCodeTupleFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.PackageRef];
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.PackageRef, value);
        }

        public string TargetCode
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.TargetCode];
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.TargetCode, value);
        }

        public WixBundlePatchTargetCodeAttributes Attributes
        {
            get => (WixBundlePatchTargetCodeAttributes)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.Attributes, (int)value);
        }

        public bool TargetsProductCode => (this.Attributes & WixBundlePatchTargetCodeAttributes.TargetsProductCode) == WixBundlePatchTargetCodeAttributes.TargetsProductCode;

        public bool TargetsUpgradeCode => (this.Attributes & WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode) == WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode;
    }
}
