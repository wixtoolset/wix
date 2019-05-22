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
                new IntermediateFieldDefinition(nameof(IsolatedComponentTupleFields.Component_Shared), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IsolatedComponentTupleFields.Component_Application), IntermediateFieldType.String),
            },
            typeof(IsolatedComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum IsolatedComponentTupleFields
    {
        Component_Shared,
        Component_Application,
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

        public string Component_Shared
        {
            get => (string)this.Fields[(int)IsolatedComponentTupleFields.Component_Shared];
            set => this.Set((int)IsolatedComponentTupleFields.Component_Shared, value);
        }

        public string Component_Application
        {
            get => (string)this.Fields[(int)IsolatedComponentTupleFields.Component_Application];
            set => this.Set((int)IsolatedComponentTupleFields.Component_Application, value);
        }
    }
}
