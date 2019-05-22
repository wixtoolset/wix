// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ODBCAttribute = new IntermediateTupleDefinition(
            TupleDefinitionType.ODBCAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCAttributeTupleFields.DriverRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCAttributeTupleFields.Attribute), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCAttributeTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ODBCAttributeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCAttributeTupleFields
    {
        DriverRef,
        Attribute,
        Value,
    }

    public class ODBCAttributeTuple : IntermediateTuple
    {
        public ODBCAttributeTuple() : base(TupleDefinitions.ODBCAttribute, null, null)
        {
        }

        public ODBCAttributeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ODBCAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCAttributeTupleFields index] => this.Fields[(int)index];

        public string DriverRef
        {
            get => (string)this.Fields[(int)ODBCAttributeTupleFields.DriverRef];
            set => this.Set((int)ODBCAttributeTupleFields.DriverRef, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)ODBCAttributeTupleFields.Attribute];
            set => this.Set((int)ODBCAttributeTupleFields.Attribute, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ODBCAttributeTupleFields.Value];
            set => this.Set((int)ODBCAttributeTupleFields.Value, value);
        }
    }
}