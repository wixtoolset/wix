// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusMethod = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusMethod.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusMethodTupleFields.InterfaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodTupleFields.Index), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComPlusMethodTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusMethodTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusMethodTupleFields
    {
        InterfaceRef,
        Index,
        Name,
    }

    public class ComPlusMethodTuple : IntermediateTuple
    {
        public ComPlusMethodTuple() : base(ComPlusTupleDefinitions.ComPlusMethod, null, null)
        {
        }

        public ComPlusMethodTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusMethod, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusMethodTupleFields index] => this.Fields[(int)index];

        public string InterfaceRef
        {
            get => this.Fields[(int)ComPlusMethodTupleFields.InterfaceRef].AsString();
            set => this.Set((int)ComPlusMethodTupleFields.InterfaceRef, value);
        }

        public int Index
        {
            get => this.Fields[(int)ComPlusMethodTupleFields.Index].AsNumber();
            set => this.Set((int)ComPlusMethodTupleFields.Index, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusMethodTupleFields.Name].AsString();
            set => this.Set((int)ComPlusMethodTupleFields.Name, value);
        }
    }
}