// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ODBCDataSource = new IntermediateTupleDefinition(
            TupleDefinitionType.ODBCDataSource,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.DataSource), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.DriverDescription), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.Registration), IntermediateFieldType.Number),
            },
            typeof(ODBCDataSourceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCDataSourceTupleFields
    {
        DataSource,
        Component_,
        Description,
        DriverDescription,
        Registration,
    }

    public class ODBCDataSourceTuple : IntermediateTuple
    {
        public ODBCDataSourceTuple() : base(TupleDefinitions.ODBCDataSource, null, null)
        {
        }

        public ODBCDataSourceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ODBCDataSource, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCDataSourceTupleFields index] => this.Fields[(int)index];

        public string DataSource
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.DataSource]?.Value;
            set => this.Set((int)ODBCDataSourceTupleFields.DataSource, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.Component_]?.Value;
            set => this.Set((int)ODBCDataSourceTupleFields.Component_, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.Description]?.Value;
            set => this.Set((int)ODBCDataSourceTupleFields.Description, value);
        }

        public string DriverDescription
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.DriverDescription]?.Value;
            set => this.Set((int)ODBCDataSourceTupleFields.DriverDescription, value);
        }

        public int Registration
        {
            get => (int)this.Fields[(int)ODBCDataSourceTupleFields.Registration]?.Value;
            set => this.Set((int)ODBCDataSourceTupleFields.Registration, value);
        }
    }
}