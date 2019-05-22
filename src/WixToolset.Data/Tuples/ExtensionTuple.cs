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
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.ProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.MimeRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionTupleFields.FeatureRef), IntermediateFieldType.String),
            },
            typeof(ExtensionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ExtensionTupleFields
    {
        Extension,
        ComponentRef,
        ProgIdRef,
        MimeRef,
        FeatureRef,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.ComponentRef];
            set => this.Set((int)ExtensionTupleFields.ComponentRef, value);
        }

        public string ProgIdRef
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.ProgIdRef];
            set => this.Set((int)ExtensionTupleFields.ProgIdRef, value);
        }

        public string MimeRef
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.MimeRef];
            set => this.Set((int)ExtensionTupleFields.MimeRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)ExtensionTupleFields.FeatureRef];
            set => this.Set((int)ExtensionTupleFields.FeatureRef, value);
        }
    }
}