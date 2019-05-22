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
                new IntermediateFieldDefinition(nameof(ODBCDataSourceTupleFields.ComponentRef), IntermediateFieldType.String),
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
        ComponentRef,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.ComponentRef];
            set => this.Set((int)ODBCDataSourceTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.Description];
            set => this.Set((int)ODBCDataSourceTupleFields.Description, value);
        }

        public string DriverDescription
        {
            get => (string)this.Fields[(int)ODBCDataSourceTupleFields.DriverDescription];
            set => this.Set((int)ODBCDataSourceTupleFields.DriverDescription, value);
        }

        public int Registration
        {
            get => (int)this.Fields[(int)ODBCDataSourceTupleFields.Registration];
            set => this.Set((int)ODBCDataSourceTupleFields.Registration, value);
        }
    }
}
