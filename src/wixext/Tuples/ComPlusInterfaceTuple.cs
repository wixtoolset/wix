// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusInterface = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusInterface.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusInterfaceTupleFields.Interface), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfaceTupleFields.ComPlusComponent_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfaceTupleFields.IID), IntermediateFieldType.String),
            },
            typeof(ComPlusInterfaceTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusInterfaceTupleFields
    {
        Interface,
        ComPlusComponent_,
        IID,
    }

    public class ComPlusInterfaceTuple : IntermediateTuple
    {
        public ComPlusInterfaceTuple() : base(ComPlusTupleDefinitions.ComPlusInterface, null, null)
        {
        }

        public ComPlusInterfaceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusInterface, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusInterfaceTupleFields index] => this.Fields[(int)index];

        public string Interface
        {
            get => this.Fields[(int)ComPlusInterfaceTupleFields.Interface].AsString();
            set => this.Set((int)ComPlusInterfaceTupleFields.Interface, value);
        }

        public string ComPlusComponent_
        {
            get => this.Fields[(int)ComPlusInterfaceTupleFields.ComPlusComponent_].AsString();
            set => this.Set((int)ComPlusInterfaceTupleFields.ComPlusComponent_, value);
        }

        public string IID
        {
            get => this.Fields[(int)ComPlusInterfaceTupleFields.IID].AsString();
            set => this.Set((int)ComPlusInterfaceTupleFields.IID, value);
        }
    }
}