// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ServiceControl = new IntermediateTupleDefinition(
            TupleDefinitionType.ServiceControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.InstallRemove), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.UninstallRemove), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.InstallStart), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.UninstallStart), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.InstallStop), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.UninstallStop), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Wait), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ServiceControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ServiceControlTupleFields
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

    public class ServiceControlTuple : IntermediateTuple
    {
        public ServiceControlTuple() : base(TupleDefinitions.ServiceControl, null, null)
        {
        }

        public ServiceControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ServiceControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceControlTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.Name];
            set => this.Set((int)ServiceControlTupleFields.Name, value);
        }

        public bool InstallRemove
        {
            get => this.Fields[(int)ServiceControlTupleFields.InstallRemove].AsBool();
            set => this.Set((int)ServiceControlTupleFields.InstallRemove, value);
        }

        public bool UninstallRemove
        {
            get => this.Fields[(int)ServiceControlTupleFields.UninstallRemove].AsBool();
            set => this.Set((int)ServiceControlTupleFields.UninstallRemove, value);
        }

        public bool InstallStart
        {
            get => this.Fields[(int)ServiceControlTupleFields.InstallStart].AsBool();
            set => this.Set((int)ServiceControlTupleFields.InstallStart, value);
        }

        public bool UninstallStart
        {
            get => this.Fields[(int)ServiceControlTupleFields.UninstallStart].AsBool();
            set => this.Set((int)ServiceControlTupleFields.UninstallStart, value);
        }

        public bool InstallStop
        {
            get => this.Fields[(int)ServiceControlTupleFields.InstallStop].AsBool();
            set => this.Set((int)ServiceControlTupleFields.InstallStop, value);
        }

        public bool UninstallStop
        {
            get => this.Fields[(int)ServiceControlTupleFields.UninstallStop].AsBool();
            set => this.Set((int)ServiceControlTupleFields.UninstallStop, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.Arguments];
            set => this.Set((int)ServiceControlTupleFields.Arguments, value);
        }

        public bool? Wait
        {
            get => this.Fields[(int)ServiceControlTupleFields.Wait].AsNullableBool();
            set => this.Set((int)ServiceControlTupleFields.Wait, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.ComponentRef];
            set => this.Set((int)ServiceControlTupleFields.ComponentRef, value);
        }
    }
}