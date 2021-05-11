// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ServiceInstall = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ServiceInstall,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.ServiceType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.StartType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.ErrorControl), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.LoadOrderGroup), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Dependencies), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.StartName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Password), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Interactive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceInstallSymbolFields.Vital), IntermediateFieldType.Bool),
            },
            typeof(ServiceInstallSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ServiceInstallSymbolFields
    {
        Name,
        DisplayName,
        ServiceType,
        StartType,
        ErrorControl,
        LoadOrderGroup,
        Dependencies,
        StartName,
        Password,
        Arguments,
        ComponentRef,
        Description,
        Interactive,
        Vital,
    }

    public enum ServiceType
    {
        KernelDriver,
        SystemDriver,
        OwnProcess = 0x10,
        ShareProcess = 0x20,
        InteractiveProcess = 0x100,
    }

    public enum ServiceStartType
    {
        Boot,
        System,
        Auto,
        Demand,
        Disabled,
    }

    public enum ServiceErrorControl
    {
        Ignore,
        Normal,
        Critical = 3,
    }

    public class ServiceInstallSymbol : IntermediateSymbol
    {
        public ServiceInstallSymbol() : base(SymbolDefinitions.ServiceInstall, null, null)
        {
        }

        public ServiceInstallSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ServiceInstall, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceInstallSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.Name];
            set => this.Set((int)ServiceInstallSymbolFields.Name, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.DisplayName];
            set => this.Set((int)ServiceInstallSymbolFields.DisplayName, value);
        }

        public ServiceType ServiceType
        {
            get => (ServiceType)this.Fields[(int)ServiceInstallSymbolFields.ServiceType].AsNumber();
            set => this.Set((int)ServiceInstallSymbolFields.ServiceType, (int)value);
        }

        public ServiceStartType StartType
        {
            get => (ServiceStartType)this.Fields[(int)ServiceInstallSymbolFields.StartType].AsNumber();
            set => this.Set((int)ServiceInstallSymbolFields.StartType, (int)value);
        }

        public ServiceErrorControl ErrorControl
        {
            get => (ServiceErrorControl)this.Fields[(int)ServiceInstallSymbolFields.ErrorControl].AsNumber();
            set => this.Set((int)ServiceInstallSymbolFields.ErrorControl, (int)value);
        }

        public string LoadOrderGroup
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.LoadOrderGroup];
            set => this.Set((int)ServiceInstallSymbolFields.LoadOrderGroup, value);
        }

        public string Dependencies
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.Dependencies];
            set => this.Set((int)ServiceInstallSymbolFields.Dependencies, value);
        }

        public string StartName
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.StartName];
            set => this.Set((int)ServiceInstallSymbolFields.StartName, value);
        }

        public string Password
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.Password];
            set => this.Set((int)ServiceInstallSymbolFields.Password, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.Arguments];
            set => this.Set((int)ServiceInstallSymbolFields.Arguments, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.ComponentRef];
            set => this.Set((int)ServiceInstallSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ServiceInstallSymbolFields.Description];
            set => this.Set((int)ServiceInstallSymbolFields.Description, value);
        }

        public bool Interactive
        {
            get => this.Fields[(int)ServiceInstallSymbolFields.Interactive].AsBool();
            set => this.Set((int)ServiceInstallSymbolFields.Interactive, value);
        }

        public bool Vital
        {
            get => this.Fields[(int)ServiceInstallSymbolFields.Vital].AsBool();
            set => this.Set((int)ServiceInstallSymbolFields.Vital, value);
        }
    }
}