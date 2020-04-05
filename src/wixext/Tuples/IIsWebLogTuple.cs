// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebLog = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebLog.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebLogTupleFields.Format), IntermediateFieldType.String),
            },
            typeof(IIsWebLogTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebLogTupleFields
    {
        Format,
    }

    public class IIsWebLogTuple : IntermediateTuple
    {
        public IIsWebLogTuple() : base(IisTupleDefinitions.IIsWebLog, null, null)
        {
        }

        public IIsWebLogTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebLog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebLogTupleFields index] => this.Fields[(int)index];

        public string Format
        {
            get => this.Fields[(int)IIsWebLogTupleFields.Format].AsString();
            set => this.Set((int)IIsWebLogTupleFields.Format, value);
        }
    }
}