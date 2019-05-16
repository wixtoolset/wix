// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ODBCDriver = new IntermediateTupleDefinition(
            TupleDefinitionType.ODBCDriver,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.Driver), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.File_Setup), IntermediateFieldType.String),
            },
            typeof(ODBCDriverTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCDriverTupleFields
    {
        Driver,
        Component_,
        Description,
        File_,
        File_Setup,
    }

    public class ODBCDriverTuple : IntermediateTuple
    {
        public ODBCDriverTuple() : base(TupleDefinitions.ODBCDriver, null, null)
        {
        }

        public ODBCDriverTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ODBCDriver, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCDriverTupleFields index] => this.Fields[(int)index];

        public string Driver
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.Driver];
            set => this.Set((int)ODBCDriverTupleFields.Driver, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.Component_];
            set => this.Set((int)ODBCDriverTupleFields.Component_, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.Description];
            set => this.Set((int)ODBCDriverTupleFields.Description, value);
        }

        public string File_
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.File_];
            set => this.Set((int)ODBCDriverTupleFields.File_, value);
        }

        public string File_Setup
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.File_Setup];
            set => this.Set((int)ODBCDriverTupleFields.File_Setup, value);
        }
    }
}