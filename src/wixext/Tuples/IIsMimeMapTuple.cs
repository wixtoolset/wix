// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsMimeMap = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsMimeMap.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsMimeMapTupleFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsMimeMapTupleFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsMimeMapTupleFields.MimeType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsMimeMapTupleFields.Extension), IntermediateFieldType.String),
            },
            typeof(IIsMimeMapTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsMimeMapTupleFields
    {
        ParentType,
        ParentValue,
        MimeType,
        Extension,
    }

    public class IIsMimeMapTuple : IntermediateTuple
    {
        public IIsMimeMapTuple() : base(IisTupleDefinitions.IIsMimeMap, null, null)
        {
        }

        public IIsMimeMapTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsMimeMap, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsMimeMapTupleFields index] => this.Fields[(int)index];

        public int ParentType
        {
            get => this.Fields[(int)IIsMimeMapTupleFields.ParentType].AsNumber();
            set => this.Set((int)IIsMimeMapTupleFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsMimeMapTupleFields.ParentValue].AsString();
            set => this.Set((int)IIsMimeMapTupleFields.ParentValue, value);
        }

        public string MimeType
        {
            get => this.Fields[(int)IIsMimeMapTupleFields.MimeType].AsString();
            set => this.Set((int)IIsMimeMapTupleFields.MimeType, value);
        }

        public string Extension
        {
            get => this.Fields[(int)IIsMimeMapTupleFields.Extension].AsString();
            set => this.Set((int)IIsMimeMapTupleFields.Extension, value);
        }
    }
}