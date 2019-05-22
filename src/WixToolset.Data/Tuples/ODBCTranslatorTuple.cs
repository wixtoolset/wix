// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ODBCTranslator = new IntermediateTupleDefinition(
            TupleDefinitionType.ODBCTranslator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.SetupFileRef), IntermediateFieldType.String),
            },
            typeof(ODBCTranslatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCTranslatorTupleFields
    {
        ComponentRef,
        Description,
        FileRef,
        SetupFileRef,
    }

    public class ODBCTranslatorTuple : IntermediateTuple
    {
        public ODBCTranslatorTuple() : base(TupleDefinitions.ODBCTranslator, null, null)
        {
        }

        public ODBCTranslatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ODBCTranslator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCTranslatorTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.ComponentRef];
            set => this.Set((int)ODBCTranslatorTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.Description];
            set => this.Set((int)ODBCTranslatorTupleFields.Description, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.FileRef];
            set => this.Set((int)ODBCTranslatorTupleFields.FileRef, value);
        }

        public string SetupFileRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.SetupFileRef];
            set => this.Set((int)ODBCTranslatorTupleFields.SetupFileRef, value);
        }
    }
}
