// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchHeaders = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchHeaders,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchHeadersTupleFields.StreamRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchHeadersTupleFields.Header), IntermediateFieldType.Path),
            },
            typeof(MsiPatchHeadersTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchHeadersTupleFields
    {
        StreamRef,
        Header,
    }

    public class MsiPatchHeadersTuple : IntermediateTuple
    {
        public MsiPatchHeadersTuple() : base(TupleDefinitions.MsiPatchHeaders, null, null)
        {
        }

        public MsiPatchHeadersTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchHeaders, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchHeadersTupleFields index] => this.Fields[(int)index];

        public string StreamRef
        {
            get => (string)this.Fields[(int)MsiPatchHeadersTupleFields.StreamRef]?.Value;
            set => this.Set((int)MsiPatchHeadersTupleFields.StreamRef, value);
        }

        public string Header
        {
            get => (string)this.Fields[(int)MsiPatchHeadersTupleFields.Header]?.Value;
            set => this.Set((int)MsiPatchHeadersTupleFields.Header, value);
        }
    }
}