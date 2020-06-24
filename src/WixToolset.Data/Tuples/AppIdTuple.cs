// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition AppId = new IntermediateSymbolDefinition(
            SymbolDefinitionType.AppId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.AppId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.RemoteServerName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.LocalService), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.ServiceParameters), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.DllSurrogate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.ActivateAtStorage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(AppIdSymbolFields.RunAsInteractiveUser), IntermediateFieldType.Number),
            },
            typeof(AppIdSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum AppIdSymbolFields
    {
        AppId,
        RemoteServerName,
        LocalService,
        ServiceParameters,
        DllSurrogate,
        ActivateAtStorage,
        RunAsInteractiveUser,
    }

    public class AppIdSymbol : IntermediateSymbol
    {
        public AppIdSymbol() : base(SymbolDefinitions.AppId, null, null)
        {
        }

        public AppIdSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.AppId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AppIdSymbolFields index] => this.Fields[(int)index];

        public string AppId
        {
            get => (string)this.Fields[(int)AppIdSymbolFields.AppId];
            set => this.Set((int)AppIdSymbolFields.AppId, value);
        }

        public string RemoteServerName
        {
            get => (string)this.Fields[(int)AppIdSymbolFields.RemoteServerName];
            set => this.Set((int)AppIdSymbolFields.RemoteServerName, value);
        }

        public string LocalService
        {
            get => (string)this.Fields[(int)AppIdSymbolFields.LocalService];
            set => this.Set((int)AppIdSymbolFields.LocalService, value);
        }

        public string ServiceParameters
        {
            get => (string)this.Fields[(int)AppIdSymbolFields.ServiceParameters];
            set => this.Set((int)AppIdSymbolFields.ServiceParameters, value);
        }

        public string DllSurrogate
        {
            get => (string)this.Fields[(int)AppIdSymbolFields.DllSurrogate];
            set => this.Set((int)AppIdSymbolFields.DllSurrogate, value);
        }

        public bool? ActivateAtStorage
        {
            get => (bool?)this.Fields[(int)AppIdSymbolFields.ActivateAtStorage];
            set => this.Set((int)AppIdSymbolFields.ActivateAtStorage, value);
        }

        public bool? RunAsInteractiveUser
        {
            get => (bool?)this.Fields[(int)AppIdSymbolFields.RunAsInteractiveUser];
            set => this.Set((int)AppIdSymbolFields.RunAsInteractiveUser, value);
        }
    }
}