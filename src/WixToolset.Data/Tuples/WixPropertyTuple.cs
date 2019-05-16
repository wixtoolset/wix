// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixProperty = new IntermediateTupleDefinition(
            TupleDefinitionType.WixProperty,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPropertyTupleFields.Property_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPropertyTupleFields.Admin), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPropertyTupleFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPropertyTupleFields.Secure), IntermediateFieldType.Bool),
            },
            typeof(WixPropertyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPropertyTupleFields
    {
        Property_,
        Admin,
        Hidden,
        Secure,
    }

    public class WixPropertyTuple : IntermediateTuple
    {
        public WixPropertyTuple() : base(TupleDefinitions.WixProperty, null, null)
        {
        }

        public WixPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPropertyTupleFields index] => this.Fields[(int)index];

        public string Property_
        {
            get => (string)this.Fields[(int)WixPropertyTupleFields.Property_];
            set => this.Set((int)WixPropertyTupleFields.Property_, value);
        }

        public bool Admin
        {
            get => (bool)this.Fields[(int)WixPropertyTupleFields.Admin];
            set => this.Set((int)WixPropertyTupleFields.Admin, value);
        }

        public bool Hidden
        {
            get => (bool)this.Fields[(int)WixPropertyTupleFields.Hidden];
            set => this.Set((int)WixPropertyTupleFields.Hidden, value);
        }

        public bool Secure
        {
            get => (bool)this.Fields[(int)WixPropertyTupleFields.Secure];
            set => this.Set((int)WixPropertyTupleFields.Secure, value);
        }
    }
}