// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ServiceInstall = new IntermediateTupleDefinition(
            TupleDefinitionType.ServiceInstall,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.ServiceType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.StartType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.ErrorControl), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.LoadOrderGroup), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Dependencies), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.StartName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Password), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Interactive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Vital), IntermediateFieldType.Bool),
            },
            typeof(ServiceInstallTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ServiceInstallTupleFields
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
        OwnProcess = 4,
        ShareProcess = 8,
        InteractiveProcess = 256,
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

    public class ServiceInstallTuple : IntermediateTuple
    {
        public ServiceInstallTuple() : base(TupleDefinitions.ServiceInstall, null, null)
        {
        }

        public ServiceInstallTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ServiceInstall, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceInstallTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Name];
            set => this.Set((int)ServiceInstallTupleFields.Name, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.DisplayName];
            set => this.Set((int)ServiceInstallTupleFields.DisplayName, value);
        }

        public ServiceType ServiceType
        {
            get => (ServiceType)this.Fields[(int)ServiceInstallTupleFields.ServiceType].AsNumber();
            set => this.Set((int)ServiceInstallTupleFields.ServiceType, (int)value);
        }

        public ServiceStartType StartType
        {
            get => (ServiceStartType)this.Fields[(int)ServiceInstallTupleFields.StartType].AsNumber();
            set => this.Set((int)ServiceInstallTupleFields.StartType, (int)value);
        }

        public ServiceErrorControl ErrorControl
        {
            get => (ServiceErrorControl)this.Fields[(int)ServiceInstallTupleFields.ErrorControl].AsNumber();
            set => this.Set((int)ServiceInstallTupleFields.ErrorControl, (int)value);
        }

        public string LoadOrderGroup
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.LoadOrderGroup];
            set => this.Set((int)ServiceInstallTupleFields.LoadOrderGroup, value);
        }

        public string Dependencies
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Dependencies];
            set => this.Set((int)ServiceInstallTupleFields.Dependencies, value);
        }

        public string StartName
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.StartName];
            set => this.Set((int)ServiceInstallTupleFields.StartName, value);
        }

        public string Password
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Password];
            set => this.Set((int)ServiceInstallTupleFields.Password, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Arguments];
            set => this.Set((int)ServiceInstallTupleFields.Arguments, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.ComponentRef];
            set => this.Set((int)ServiceInstallTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Description];
            set => this.Set((int)ServiceInstallTupleFields.Description, value);
        }

        public bool Interactive
        {
            get => this.Fields[(int)ServiceInstallTupleFields.Interactive].AsBool();
            set => this.Set((int)ServiceInstallTupleFields.Interactive, value);
        }

        public bool Vital
        {
            get => this.Fields[(int)ServiceInstallTupleFields.Vital].AsBool();
            set => this.Set((int)ServiceInstallTupleFields.Vital, value);
        }
    }
}