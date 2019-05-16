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
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.TargetCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundlePatchTargetCodeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePatchTargetCodeTupleFields
    {
        PackageId,
        TargetCode,
        Attributes,
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

        public string PackageId
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.PackageId];
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.PackageId, value);
        }

        public string TargetCode
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.TargetCode];
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.TargetCode, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixBundlePatchTargetCodeTupleFields.Attributes];
            set => this.Set((int)WixBundlePatchTargetCodeTupleFields.Attributes, value);
        }
    }
}