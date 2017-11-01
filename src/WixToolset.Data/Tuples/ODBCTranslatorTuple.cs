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
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.Translator), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorTupleFields.File_Setup), IntermediateFieldType.String),
            },
            typeof(ODBCTranslatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ODBCTranslatorTupleFields
    {
        Translator,
        Component_,
        Description,
        File_,
        File_Setup,
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

        public string Translator
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.Translator]?.Value;
            set => this.Set((int)ODBCTranslatorTupleFields.Translator, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.Component_]?.Value;
            set => this.Set((int)ODBCTranslatorTupleFields.Component_, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.Description]?.Value;
            set => this.Set((int)ODBCTranslatorTupleFields.Description, value);
        }

        public string File_
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.File_]?.Value;
            set => this.Set((int)ODBCTranslatorTupleFields.File_, value);
        }

        public string File_Setup
        {
            get => (string)this.Fields[(int)ODBCTranslatorTupleFields.File_Setup]?.Value;
            set => this.Set((int)ODBCTranslatorTupleFields.File_Setup, value);
        }
    }
}