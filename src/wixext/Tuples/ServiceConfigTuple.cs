// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ServiceConfig = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.ServiceConfig.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.ServiceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.NewService), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.FirstFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.SecondFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.ThirdFailureActionType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.ResetPeriodInDays), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.RestartServiceDelayInSeconds), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.ProgramCommandLine), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceConfigSymbolFields.RebootMessage), IntermediateFieldType.String),
            },
            typeof(ServiceConfigSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum ServiceConfigSymbolFields
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

    public class ServiceConfigSymbol : IntermediateSymbol
    {
        public ServiceConfigSymbol() : base(UtilSymbolDefinitions.ServiceConfig, null, null)
        {
        }

        public ServiceConfigSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.ServiceConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ServiceConfigSymbolFields index] => this.Fields[(int)index];

        public string ServiceName
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.ServiceName].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.ServiceName, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.ComponentRef, value);
        }

        public int NewService
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.NewService].AsNumber();
            set => this.Set((int)ServiceConfigSymbolFields.NewService, value);
        }

        public string FirstFailureActionType
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.FirstFailureActionType].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.FirstFailureActionType, value);
        }

        public string SecondFailureActionType
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.SecondFailureActionType].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.SecondFailureActionType, value);
        }

        public string ThirdFailureActionType
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.ThirdFailureActionType].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.ThirdFailureActionType, value);
        }

        public int? ResetPeriodInDays
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.ResetPeriodInDays].AsNullableNumber();
            set => this.Set((int)ServiceConfigSymbolFields.ResetPeriodInDays, value);
        }

        public int? RestartServiceDelayInSeconds
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.RestartServiceDelayInSeconds].AsNullableNumber();
            set => this.Set((int)ServiceConfigSymbolFields.RestartServiceDelayInSeconds, value);
        }

        public string ProgramCommandLine
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.ProgramCommandLine].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.ProgramCommandLine, value);
        }

        public string RebootMessage
        {
            get => this.Fields[(int)ServiceConfigSymbolFields.RebootMessage].AsString();
            set => this.Set((int)ServiceConfigSymbolFields.RebootMessage, value);
        }
    }
}