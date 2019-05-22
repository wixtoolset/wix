// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Complus = new IntermediateTupleDefinition(
            TupleDefinitionType.Complus,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComplusTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComplusTupleFields.ExpType), IntermediateFieldType.Number),
            },
            typeof(ComplusTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ComplusTupleFields
    {
        ComponentRef,
        ExpType,
    }

    public class ComplusTuple : IntermediateTuple
    {
        public ComplusTuple() : base(TupleDefinitions.Complus, null, null)
        {
        }

        public ComplusTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Complus, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComplusTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ComplusTupleFields.ComponentRef];
            set => this.Set((int)ComplusTupleFields.ComponentRef, value);
        }

        public int ExpType
        {
            get => (int)this.Fields[(int)ComplusTupleFields.ExpType];
            set => this.Set((int)ComplusTupleFields.ExpType, value);
        }
    }
}