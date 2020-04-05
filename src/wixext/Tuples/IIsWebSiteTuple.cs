// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebSite = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebSite.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.ConnectionTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.State), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.KeyAddressRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.LogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.WebsiteId), IntermediateFieldType.String),
            },
            typeof(IIsWebSiteTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebSiteTupleFields
    {
        ComponentRef,
        Description,
        ConnectionTimeout,
        DirectoryRef,
        State,
        Attributes,
        KeyAddressRef,
        DirPropertiesRef,
        ApplicationRef,
        Sequence,
        LogRef,
        WebsiteId,
    }

    public class IIsWebSiteTuple : IntermediateTuple
    {
        public IIsWebSiteTuple() : base(IisTupleDefinitions.IIsWebSite, null, null)
        {
        }

        public IIsWebSiteTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebSite, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebSiteTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Description].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Description, value);
        }

        public int ConnectionTimeout
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.ConnectionTimeout].AsNumber();
            set => this.Set((int)IIsWebSiteTupleFields.ConnectionTimeout, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.DirectoryRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.DirectoryRef, value);
        }

        public int State
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.State].AsNumber();
            set => this.Set((int)IIsWebSiteTupleFields.State, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebSiteTupleFields.Attributes, value);
        }

        public string KeyAddressRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.KeyAddressRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.KeyAddressRef, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.ApplicationRef, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Sequence].AsNumber();
            set => this.Set((int)IIsWebSiteTupleFields.Sequence, value);
        }

        public string LogRef
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.LogRef].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.LogRef, value);
        }

        public string WebsiteId
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.WebsiteId].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.WebsiteId, value);
        }
    }
}