// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixComponentSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.WixComponentSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComponentSearchTupleFields.Guid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComponentSearchTupleFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComponentSearchTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixComponentSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixComponentSearchTupleFields
    {
        Guid,
        ProductCode,
        Attributes,
    }

    [Flags]
    public enum WixComponentSearchAttributes
    {
        KeyPath = 0x1,
        State = 0x2,
        WantDirectory = 0x4,
    }

    public class WixComponentSearchTuple : IntermediateTuple
    {
        public WixComponentSearchTuple() : base(TupleDefinitions.WixComponentSearch, null, null)
        {
        }

        public WixComponentSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixComponentSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComponentSearchTupleFields index] => this.Fields[(int)index];

        public string Guid
        {
            get => (string)this.Fields[(int)WixComponentSearchTupleFields.Guid];
            set => this.Set((int)WixComponentSearchTupleFields.Guid, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixComponentSearchTupleFields.ProductCode];
            set => this.Set((int)WixComponentSearchTupleFields.ProductCode, value);
        }

        public WixComponentSearchAttributes Attributes
        {
            get => (WixComponentSearchAttributes)this.Fields[(int)WixComponentSearchTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixComponentSearchTupleFields.Attributes, (int)value);
        }
    }
}
