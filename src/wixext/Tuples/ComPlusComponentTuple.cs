// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusComponent = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusComponent.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusComponentTupleFields.ComPlusComponent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentTupleFields.Assembly_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentTupleFields.CLSID), IntermediateFieldType.String),
            },
            typeof(ComPlusComponentTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusComponentTupleFields
    {
        ComPlusComponent,
        Assembly_,
        CLSID,
    }

    public class ComPlusComponentTuple : IntermediateTuple
    {
        public ComPlusComponentTuple() : base(ComPlusTupleDefinitions.ComPlusComponent, null, null)
        {
        }

        public ComPlusComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusComponentTupleFields index] => this.Fields[(int)index];

        public string ComPlusComponent
        {
            get => this.Fields[(int)ComPlusComponentTupleFields.ComPlusComponent].AsString();
            set => this.Set((int)ComPlusComponentTupleFields.ComPlusComponent, value);
        }

        public string Assembly_
        {
            get => this.Fields[(int)ComPlusComponentTupleFields.Assembly_].AsString();
            set => this.Set((int)ComPlusComponentTupleFields.Assembly_, value);
        }

        public string CLSID
        {
            get => this.Fields[(int)ComPlusComponentTupleFields.CLSID].AsString();
            set => this.Set((int)ComPlusComponentTupleFields.CLSID, value);
        }
    }
}