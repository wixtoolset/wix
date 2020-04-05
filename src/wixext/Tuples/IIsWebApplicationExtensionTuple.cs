// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebApplicationExtension = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebApplicationExtension.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionTupleFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionTupleFields.Extension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionTupleFields.Verbs), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionTupleFields.Executable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(IIsWebApplicationExtensionTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebApplicationExtensionTupleFields
    {
        ApplicationRef,
        Extension,
        Verbs,
        Executable,
        Attributes,
    }

    public class IIsWebApplicationExtensionTuple : IntermediateTuple
    {
        public IIsWebApplicationExtensionTuple() : base(IisTupleDefinitions.IIsWebApplicationExtension, null, null)
        {
        }

        public IIsWebApplicationExtensionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebApplicationExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebApplicationExtensionTupleFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebApplicationExtensionTupleFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebApplicationExtensionTupleFields.ApplicationRef, value);
        }

        public string Extension
        {
            get => this.Fields[(int)IIsWebApplicationExtensionTupleFields.Extension].AsString();
            set => this.Set((int)IIsWebApplicationExtensionTupleFields.Extension, value);
        }

        public string Verbs
        {
            get => this.Fields[(int)IIsWebApplicationExtensionTupleFields.Verbs].AsString();
            set => this.Set((int)IIsWebApplicationExtensionTupleFields.Verbs, value);
        }

        public string Executable
        {
            get => this.Fields[(int)IIsWebApplicationExtensionTupleFields.Executable].AsString();
            set => this.Set((int)IIsWebApplicationExtensionTupleFields.Executable, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebApplicationExtensionTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebApplicationExtensionTupleFields.Attributes, value);
        }
    }
}