// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IsolatedComponent = new IntermediateTupleDefinition(
            TupleDefinitionType.IsolatedComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IsolatedComponentTupleFields.SharedComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IsolatedComponentTupleFields.ApplicationComponentRef), IntermediateFieldType.String),
            },
            typeof(IsolatedComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum IsolatedComponentTupleFields
    {
        SharedComponentRef,
        ApplicationComponentRef,
    }

    public class IsolatedComponentTuple : IntermediateTuple
    {
        public IsolatedComponentTuple() : base(TupleDefinitions.IsolatedComponent, null, null)
        {
        }

        public IsolatedComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.IsolatedComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IsolatedComponentTupleFields index] => this.Fields[(int)index];

        public string SharedComponentRef
        {
            get => (string)this.Fields[(int)IsolatedComponentTupleFields.SharedComponentRef];
            set => this.Set((int)IsolatedComponentTupleFields.SharedComponentRef, value);
        }

        public string ApplicationComponentRef
        {
            get => (string)this.Fields[(int)IsolatedComponentTupleFields.ApplicationComponentRef];
            set => this.Set((int)IsolatedComponentTupleFields.ApplicationComponentRef, value);
        }
    }
}
