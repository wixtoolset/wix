// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Extension = new IntermediateTupleDefinition(
            TupleDefinitionType.Extension,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.Extension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.ProgId_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.MIME_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.Feature_), IntermediateFieldType.String),
            },
            typeof(ExtensionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ExtensionTupleFields
    {
        Extension,
        Component_,
        ProgId_,
        MIME_,
        Feature_,
    }

    public class ExtensionTuple : IntermediateTuple
    {
        public ExtensionTuple() : base(TupleDefinitions.Extension, null, null)
        {
        }

        public ExtensionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Extension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExtensionTupleFields index] => this.Fields[(int)index];

        public string Extension
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.Extension];
            set => this.Set((int)ExtensionTupleFields.Extension, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.Component_];
            set => this.Set((int)ExtensionTupleFields.Component_, value);
        }

        public string ProgId_
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.ProgId_];
            set => this.Set((int)ExtensionTupleFields.ProgId_, value);
        }

        public string MIME_
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.MIME_];
            set => this.Set((int)ExtensionTupleFields.MIME_, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.Feature_];
            set => this.Set((int)ExtensionTupleFields.Feature_, value);
        }
    }
}