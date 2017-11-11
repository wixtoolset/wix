// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchId = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchIdTupleFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchIdTupleFields.ClientPatchId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchIdTupleFields.OptimizePatchSizeForLargeFiles), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPatchIdTupleFields.ApiPatchingSymbolFlags), IntermediateFieldType.Number),
            },
            typeof(WixPatchIdTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchIdTupleFields
    {
        ProductCode,
        ClientPatchId,
        OptimizePatchSizeForLargeFiles,
        ApiPatchingSymbolFlags,
    }

    public class WixPatchIdTuple : IntermediateTuple
    {
        public WixPatchIdTuple() : base(TupleDefinitions.WixPatchId, null, null)
        {
        }

        public WixPatchIdTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchIdTupleFields index] => this.Fields[(int)index];

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixPatchIdTupleFields.ProductCode]?.Value;
            set => this.Set((int)WixPatchIdTupleFields.ProductCode, value);
        }

        public string ClientPatchId
        {
            get => (string)this.Fields[(int)WixPatchIdTupleFields.ClientPatchId]?.Value;
            set => this.Set((int)WixPatchIdTupleFields.ClientPatchId, value);
        }

        public bool OptimizePatchSizeForLargeFiles
        {
            get => (bool)this.Fields[(int)WixPatchIdTupleFields.OptimizePatchSizeForLargeFiles]?.Value;
            set => this.Set((int)WixPatchIdTupleFields.OptimizePatchSizeForLargeFiles, value);
        }

        public int ApiPatchingSymbolFlags
        {
            get => (int)this.Fields[(int)WixPatchIdTupleFields.ApiPatchingSymbolFlags]?.Value;
            set => this.Set((int)WixPatchIdTupleFields.ApiPatchingSymbolFlags, value);
        }
    }
}