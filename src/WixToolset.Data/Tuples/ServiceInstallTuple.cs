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
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.ServiceInstall), IntermediateFieldType.String),
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
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceInstallTupleFields.Description), IntermediateFieldType.String),
            },
            typeof(ServiceInstallTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ServiceInstallTupleFields
    {
        ServiceInstall,
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
        Component_,
        Description,
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

        public string ServiceInstall
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.ServiceInstall]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.ServiceInstall, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Name]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Name, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.DisplayName]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.DisplayName, value);
        }

        public int ServiceType
        {
            get => (int)this.Fields[(int)ServiceInstallTupleFields.ServiceType]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.ServiceType, value);
        }

        public int StartType
        {
            get => (int)this.Fields[(int)ServiceInstallTupleFields.StartType]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.StartType, value);
        }

        public int ErrorControl
        {
            get => (int)this.Fields[(int)ServiceInstallTupleFields.ErrorControl]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.ErrorControl, value);
        }

        public string LoadOrderGroup
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.LoadOrderGroup]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.LoadOrderGroup, value);
        }

        public string Dependencies
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Dependencies]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Dependencies, value);
        }

        public string StartName
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.StartName]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.StartName, value);
        }

        public string Password
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Password]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Password, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Arguments]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Arguments, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Component_]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Component_, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ServiceInstallTupleFields.Description]?.Value;
            set => this.Set((int)ServiceInstallTupleFields.Description, value);
        }
    }
}