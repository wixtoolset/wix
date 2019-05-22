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
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverTupleFields.SetupFileRef), IntermediateFieldType.String),
            },
            typeof(ODBCDriverTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCDriverTupleFields
    {
        ComponentRef,
        Description,
        FileRef,
        SetupFileRef,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.ComponentRef];
            set => this.Set((int)ODBCDriverTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.Description];
            set => this.Set((int)ODBCDriverTupleFields.Description, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.FileRef];
            set => this.Set((int)ODBCDriverTupleFields.FileRef, value);
        }

        public string SetupFileRef
        {
            get => (string)this.Fields[(int)ODBCDriverTupleFields.SetupFileRef];
            set => this.Set((int)ODBCDriverTupleFields.SetupFileRef, value);
        }
    }
}
