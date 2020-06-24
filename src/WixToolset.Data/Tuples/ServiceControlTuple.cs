// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ServiceControl = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ServiceControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.InstallRemove), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.UninstallRemove), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.InstallStart), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.UninstallStart), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.InstallStop), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.UninstallStop), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.Wait), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ServiceControlSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ServiceControlSymbolFields
    {
        Name,
        InstallRemove,
        UninstallRemove,
        InstallStart,
        UninstallStart,
        InstallStop,
        UninstallStop,
        Arguments,
        Wait,
        ComponentRef,
    }

    public class ServiceControlSymbol : IntermediateSymbol
    {
        public ServiceControlSymbol() : base(SymbolDefinitions.ServiceControl, null, null)
        {
        }

        public ServiceControlSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ServiceControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceControlSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)ServiceControlSymbolFields.Name];
            set => this.Set((int)ServiceControlSymbolFields.Name, value);
        }

        public bool InstallRemove
        {
            get => this.Fields[(int)ServiceControlSymbolFields.InstallRemove].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.InstallRemove, value);
        }

        public bool UninstallRemove
        {
            get => this.Fields[(int)ServiceControlSymbolFields.UninstallRemove].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.UninstallRemove, value);
        }

        public bool InstallStart
        {
            get => this.Fields[(int)ServiceControlSymbolFields.InstallStart].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.InstallStart, value);
        }

        public bool UninstallStart
        {
            get => this.Fields[(int)ServiceControlSymbolFields.UninstallStart].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.UninstallStart, value);
        }

        public bool InstallStop
        {
            get => this.Fields[(int)ServiceControlSymbolFields.InstallStop].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.InstallStop, value);
        }

        public bool UninstallStop
        {
            get => this.Fields[(int)ServiceControlSymbolFields.UninstallStop].AsBool();
            set => this.Set((int)ServiceControlSymbolFields.UninstallStop, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceControlSymbolFields.Arguments];
            set => this.Set((int)ServiceControlSymbolFields.Arguments, value);
        }

        public bool? Wait
        {
            get => this.Fields[(int)ServiceControlSymbolFields.Wait].AsNullableBool();
            set => this.Set((int)ServiceControlSymbolFields.Wait, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ServiceControlSymbolFields.ComponentRef];
            set => this.Set((int)ServiceControlSymbolFields.ComponentRef, value);
        }
    }
}