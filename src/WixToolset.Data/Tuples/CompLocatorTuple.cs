// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CompLocator = new IntermediateTupleDefinition(
            TupleDefinitionType.CompLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CompLocatorTupleFields.Signature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CompLocatorTupleFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CompLocatorTupleFields.Type), IntermediateFieldType.Number),
            },
            typeof(CompLocatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CompLocatorTupleFields
    {
        Signature_,
        ComponentId,
        Type,
    }

    public class CompLocatorTuple : IntermediateTuple
    {
        public CompLocatorTuple() : base(TupleDefinitions.CompLocator, null, null)
        {
        }

        public CompLocatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.CompLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CompLocatorTupleFields index] => this.Fields[(int)index];

        public string Signature_
        {
            get => (string)this.Fields[(int)CompLocatorTupleFields.Signature_]?.Value;
            set => this.Set((int)CompLocatorTupleFields.Signature_, value);
        }

        public string ComponentId
        {
            get => (string)this.Fields[(int)CompLocatorTupleFields.ComponentId]?.Value;
            set => this.Set((int)CompLocatorTupleFields.ComponentId, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)CompLocatorTupleFields.Type]?.Value;
            set => this.Set((int)CompLocatorTupleFields.Type, value);
        }
    }
}