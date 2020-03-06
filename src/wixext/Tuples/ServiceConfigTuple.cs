// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ServiceConfig = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.ServiceConfig.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.ServiceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.NewService), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.FirstFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.SecondFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.ThirdFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.ResetPeriodInDays), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.RestartServiceDelayInSeconds), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.ProgramCommandLine), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigTupleFields.RebootMessage), IntermediateFieldType.String),
            },
            typeof(ServiceConfigTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum ServiceConfigTupleFields
    {
        ServiceName,
        ComponentRef,
        NewService,
        FirstFailureActionType,
        SecondFailureActionType,
        ThirdFailureActionType,
        ResetPeriodInDays,
        RestartServiceDelayInSeconds,
        ProgramCommandLine,
        RebootMessage,
    }

    public class ServiceConfigTuple : IntermediateTuple
    {
        public ServiceConfigTuple() : base(UtilTupleDefinitions.ServiceConfig, null, null)
        {
        }

        public ServiceConfigTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.ServiceConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceConfigTupleFields index] => this.Fields[(int)index];

        public string ServiceName
        {
            get => this.Fields[(int)ServiceConfigTupleFields.ServiceName].AsString();
            set => this.Set((int)ServiceConfigTupleFields.ServiceName, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ServiceConfigTupleFields.ComponentRef].AsString();
            set => this.Set((int)ServiceConfigTupleFields.ComponentRef, value);
        }

        public int NewService
        {
            get => this.Fields[(int)ServiceConfigTupleFields.NewService].AsNumber();
            set => this.Set((int)ServiceConfigTupleFields.NewService, value);
        }

        public string FirstFailureActionType
        {
            get => this.Fields[(int)ServiceConfigTupleFields.FirstFailureActionType].AsString();
            set => this.Set((int)ServiceConfigTupleFields.FirstFailureActionType, value);
        }

        public string SecondFailureActionType
        {
            get => this.Fields[(int)ServiceConfigTupleFields.SecondFailureActionType].AsString();
            set => this.Set((int)ServiceConfigTupleFields.SecondFailureActionType, value);
        }

        public string ThirdFailureActionType
        {
            get => this.Fields[(int)ServiceConfigTupleFields.ThirdFailureActionType].AsString();
            set => this.Set((int)ServiceConfigTupleFields.ThirdFailureActionType, value);
        }

        public int ResetPeriodInDays
        {
            get => this.Fields[(int)ServiceConfigTupleFields.ResetPeriodInDays].AsNumber();
            set => this.Set((int)ServiceConfigTupleFields.ResetPeriodInDays, value);
        }

        public int RestartServiceDelayInSeconds
        {
            get => this.Fields[(int)ServiceConfigTupleFields.RestartServiceDelayInSeconds].AsNumber();
            set => this.Set((int)ServiceConfigTupleFields.RestartServiceDelayInSeconds, value);
        }

        public string ProgramCommandLine
        {
            get => this.Fields[(int)ServiceConfigTupleFields.ProgramCommandLine].AsString();
            set => this.Set((int)ServiceConfigTupleFields.ProgramCommandLine, value);
        }

        public string RebootMessage
        {
            get => this.Fields[(int)ServiceConfigTupleFields.RebootMessage].AsString();
            set => this.Set((int)ServiceConfigTupleFields.RebootMessage, value);
        }
    }
}