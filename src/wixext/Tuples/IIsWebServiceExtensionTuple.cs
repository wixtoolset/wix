// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebServiceExtension = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebServiceExtension.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.WebServiceExtension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.Group), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(IIsWebServiceExtensionTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebServiceExtensionTupleFields
    {
        WebServiceExtension,
        Component_,
        File,
        Description,
        Group,
        Attributes,
    }

    public class IIsWebServiceExtensionTuple : IntermediateTuple
    {
        public IIsWebServiceExtensionTuple() : base(IisTupleDefinitions.IIsWebServiceExtension, null, null)
        {
        }

        public IIsWebServiceExtensionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebServiceExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebServiceExtensionTupleFields index] => this.Fields[(int)index];

        public string WebServiceExtension
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.WebServiceExtension].AsString();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.WebServiceExtension, value);
        }

        public string Component_
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.Component_].AsString();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.Component_, value);
        }

        public string File
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.File].AsString();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.File, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.Description].AsString();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.Description, value);
        }

        public string Group
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.Group].AsString();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.Group, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebServiceExtensionTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebServiceExtensionTupleFields.Attributes, value);
        }
    }
}