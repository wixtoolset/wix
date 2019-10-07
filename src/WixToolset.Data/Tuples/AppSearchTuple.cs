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
                new IntermediateFieldDefinition(nameof(AppSearchTupleFields.PropertyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppSearchTupleFields.SignatureRef), IntermediateFieldType.String),
            },
            typeof(AppSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AppSearchTupleFields
    {
        PropertyRef,
        SignatureRef,
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

        public string PropertyRef
        {
            get => (string)this.Fields[(int)AppSearchTupleFields.PropertyRef];
            set => this.Set((int)AppSearchTupleFields.PropertyRef, value);
        }

        public string SignatureRef
        {
            get => (string)this.Fields[(int)AppSearchTupleFields.SignatureRef];
            set => this.Set((int)AppSearchTupleFields.SignatureRef, value);
        }
    }
}