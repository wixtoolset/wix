// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebError = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebError.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.ErrorCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.SubCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebErrorTupleFields.URL), IntermediateFieldType.String),
            },
            typeof(IIsWebErrorTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebErrorTupleFields
    {
        ErrorCode,
        SubCode,
        ParentType,
        ParentValue,
        File,
        URL,
    }

    public class IIsWebErrorTuple : IntermediateTuple
    {
        public IIsWebErrorTuple() : base(IisTupleDefinitions.IIsWebError, null, null)
        {
        }

        public IIsWebErrorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebError, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebErrorTupleFields index] => this.Fields[(int)index];

        public int ErrorCode
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.ErrorCode].AsNumber();
            set => this.Set((int)IIsWebErrorTupleFields.ErrorCode, value);
        }

        public int SubCode
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.SubCode].AsNumber();
            set => this.Set((int)IIsWebErrorTupleFields.SubCode, value);
        }

        public int ParentType
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.ParentType].AsNumber();
            set => this.Set((int)IIsWebErrorTupleFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.ParentValue].AsString();
            set => this.Set((int)IIsWebErrorTupleFields.ParentValue, value);
        }

        public string File
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.File].AsString();
            set => this.Set((int)IIsWebErrorTupleFields.File, value);
        }

        public string URL
        {
            get => this.Fields[(int)IIsWebErrorTupleFields.URL].AsString();
            set => this.Set((int)IIsWebErrorTupleFields.URL, value);
        }
    }
}