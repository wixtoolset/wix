// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchRef = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchRef,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchRefTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchRefTupleFields.PrimaryKeys), IntermediateFieldType.String),
            },
            typeof(WixPatchRefTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchRefTupleFields
    {
        Table,
        PrimaryKeys,
    }

    public class WixPatchRefTuple : IntermediateTuple
    {
        public WixPatchRefTuple() : base(TupleDefinitions.WixPatchRef, null, null)
        {
        }

        public WixPatchRefTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchRef, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchRefTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixPatchRefTupleFields.Table]?.Value;
            set => this.Set((int)WixPatchRefTupleFields.Table, value);
        }

        public string PrimaryKeys
        {
            get => (string)this.Fields[(int)WixPatchRefTupleFields.PrimaryKeys]?.Value;
            set => this.Set((int)WixPatchRefTupleFields.PrimaryKeys, value);
        }
    }
}