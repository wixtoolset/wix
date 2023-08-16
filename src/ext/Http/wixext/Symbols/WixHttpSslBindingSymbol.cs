// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpSslBinding = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpSslBinding.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.Host), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.Thumbprint), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.AppId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.Store), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.HandleExisting), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixHttpSslBindingSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(WixHttpSslBindingSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum WixHttpSslBindingSymbolFields
    {
        Host,
        Port,
        Thumbprint,
        AppId,
        Store,
        HandleExisting,
        ComponentRef,
    }

    public class WixHttpSslBindingSymbol : IntermediateSymbol
    {
        public WixHttpSslBindingSymbol() : base(HttpSymbolDefinitions.WixHttpSslBinding, null, null)
        {
        }

        public WixHttpSslBindingSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpSslBinding, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpSslBindingSymbolFields index] => this.Fields[(int)index];

        public string Host
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.Host].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.Host, value);
        }

        public string Port
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.Port].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.Port, value);
        }

        public string Thumbprint
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.Thumbprint].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.Thumbprint, value);
        }

        public string AppId
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.AppId].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.AppId, value);
        }

        public string Store
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.Store].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.Store, value);
        }

        public HandleExisting HandleExisting
        {
            get => (HandleExisting)this.Fields[(int)WixHttpSslBindingSymbolFields.HandleExisting].AsNumber();
            set => this.Set((int)WixHttpSslBindingSymbolFields.HandleExisting, (int)value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)WixHttpSslBindingSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixHttpSslBindingSymbolFields.ComponentRef, value);
        }
    }
}
