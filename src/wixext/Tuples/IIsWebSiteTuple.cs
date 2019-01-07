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
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Web), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.ConnectionTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.State), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.KeyAddress_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.DirProperties_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Application_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteTupleFields.Log_), IntermediateFieldType.String),
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
        Web,
        Component_,
        Description,
        ConnectionTimeout,
        Directory_,
        State,
        Attributes,
        KeyAddress_,
        DirProperties_,
        Application_,
        Sequence,
        Log_,
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

        public string Web
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Web].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Web, value);
        }

        public string Component_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Component_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Component_, value);
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

        public string Directory_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Directory_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Directory_, value);
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

        public string KeyAddress_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.KeyAddress_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.KeyAddress_, value);
        }

        public string DirProperties_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.DirProperties_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.DirProperties_, value);
        }

        public string Application_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Application_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Application_, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Sequence].AsNumber();
            set => this.Set((int)IIsWebSiteTupleFields.Sequence, value);
        }

        public string Log_
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.Log_].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.Log_, value);
        }

        public string WebsiteId
        {
            get => this.Fields[(int)IIsWebSiteTupleFields.WebsiteId].AsString();
            set => this.Set((int)IIsWebSiteTupleFields.WebsiteId, value);
        }
    }
}