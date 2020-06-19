// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleCustomDataCell = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleCustomDataCell,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellTupleFields.CustomDataRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellTupleFields.AttributeRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellTupleFields.ElementId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataCellTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleCustomDataCellTupleFields
    {
        CustomDataRef,
        AttributeRef,
        ElementId,
        Value,
    }

    public class WixBundleCustomDataCellTuple : IntermediateTuple
    {
        public WixBundleCustomDataCellTuple() : base(TupleDefinitions.WixBundleCustomDataCell, null, null)
        {
        }

        public WixBundleCustomDataCellTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleCustomDataCell, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataCellTupleFields index] => this.Fields[(int)index];

        public string CustomDataRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellTupleFields.CustomDataRef];
            set => this.Set((int)WixBundleCustomDataCellTupleFields.CustomDataRef, value);
        }

        public string AttributeRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellTupleFields.AttributeRef];
            set => this.Set((int)WixBundleCustomDataCellTupleFields.AttributeRef, value);
        }

        public string ElementId
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellTupleFields.ElementId];
            set => this.Set((int)WixBundleCustomDataCellTupleFields.ElementId, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellTupleFields.Value];
            set => this.Set((int)WixBundleCustomDataCellTupleFields.Value, value);
        }
    }
}
