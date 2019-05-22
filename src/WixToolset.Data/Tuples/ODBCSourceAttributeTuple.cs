// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ODBCSourceAttribute = new IntermediateTupleDefinition(
            TupleDefinitionType.ODBCSourceAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeTupleFields.DataSourceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeTupleFields.Attribute), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ODBCSourceAttributeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCSourceAttributeTupleFields
    {
        DataSourceRef,
        Attribute,
        Value,
    }

    public class ODBCSourceAttributeTuple : IntermediateTuple
    {
        public ODBCSourceAttributeTuple() : base(TupleDefinitions.ODBCSourceAttribute, null, null)
        {
        }

        public ODBCSourceAttributeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ODBCSourceAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCSourceAttributeTupleFields index] => this.Fields[(int)index];

        public string DataSourceRef
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeTupleFields.DataSourceRef];
            set => this.Set((int)ODBCSourceAttributeTupleFields.DataSourceRef, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeTupleFields.Attribute];
            set => this.Set((int)ODBCSourceAttributeTupleFields.Attribute, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeTupleFields.Value];
            set => this.Set((int)ODBCSourceAttributeTupleFields.Value, value);
        }
    }
}