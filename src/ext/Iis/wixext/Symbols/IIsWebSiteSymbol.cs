// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebSite = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebSite.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.ConnectionTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.State), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.KeyAddressRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.LogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteSymbolFields.WebsiteId), IntermediateFieldType.String),
            },
            typeof(IIsWebSiteSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebSiteSymbolFields
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

    public class IIsWebSiteSymbol : IntermediateSymbol
    {
        public IIsWebSiteSymbol() : base(IisSymbolDefinitions.IIsWebSite, null, null)
        {
        }

        public IIsWebSiteSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebSite, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebSiteSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.Description].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.Description, value);
        }

        public int? ConnectionTimeout
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.ConnectionTimeout].AsNullableNumber();
            set => this.Set((int)IIsWebSiteSymbolFields.ConnectionTimeout, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.DirectoryRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.DirectoryRef, value);
        }

        public int? State
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.State].AsNullableNumber();
            set => this.Set((int)IIsWebSiteSymbolFields.State, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebSiteSymbolFields.Attributes, value);
        }

        public string KeyAddressRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.KeyAddressRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.KeyAddressRef, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.ApplicationRef, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)IIsWebSiteSymbolFields.Sequence, value);
        }

        public string LogRef
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.LogRef].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.LogRef, value);
        }

        public string WebsiteId
        {
            get => this.Fields[(int)IIsWebSiteSymbolFields.WebsiteId].AsString();
            set => this.Set((int)IIsWebSiteSymbolFields.WebsiteId, value);
        }
    }
}