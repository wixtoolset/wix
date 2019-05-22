// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixInstanceComponent = new IntermediateTupleDefinition(
            TupleDefinitionType.WixInstanceComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixInstanceComponentTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(WixInstanceComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixInstanceComponentTupleFields
    {
        ComponentRef,
    }

    public class WixInstanceComponentTuple : IntermediateTuple
    {
        public WixInstanceComponentTuple() : base(TupleDefinitions.WixInstanceComponent, null, null)
        {
        }

        public WixInstanceComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixInstanceComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixInstanceComponentTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)WixInstanceComponentTupleFields.ComponentRef];
            set => this.Set((int)WixInstanceComponentTupleFields.ComponentRef, value);
        }
    }
}