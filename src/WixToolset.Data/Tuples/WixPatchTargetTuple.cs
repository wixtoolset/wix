// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchTarget = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchTarget,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchTargetTupleFields.ProductCode), IntermediateFieldType.String),
            },
            typeof(WixPatchTargetTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchTargetTupleFields
    {
        ProductCode,
    }

    public class WixPatchTargetTuple : IntermediateTuple
    {
        public WixPatchTargetTuple() : base(TupleDefinitions.WixPatchTarget, null, null)
        {
        }

        public WixPatchTargetTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchTarget, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchTargetTupleFields index] => this.Fields[(int)index];

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixPatchTargetTupleFields.ProductCode];
            set => this.Set((int)WixPatchTargetTupleFields.ProductCode, value);
        }
    }
}