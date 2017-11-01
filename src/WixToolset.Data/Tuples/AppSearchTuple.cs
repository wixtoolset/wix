// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition AppSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.AppSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AppSearchTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppSearchTupleFields.Signature_), IntermediateFieldType.String),
            },
            typeof(AppSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AppSearchTupleFields
    {
        Property,
        Signature_,
    }

    public class AppSearchTuple : IntermediateTuple
    {
        public AppSearchTuple() : base(TupleDefinitions.AppSearch, null, null)
        {
        }

        public AppSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.AppSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AppSearchTupleFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)AppSearchTupleFields.Property]?.Value;
            set => this.Set((int)AppSearchTupleFields.Property, value);
        }

        public string Signature_
        {
            get => (string)this.Fields[(int)AppSearchTupleFields.Signature_]?.Value;
            set => this.Set((int)AppSearchTupleFields.Signature_, value);
        }
    }
}