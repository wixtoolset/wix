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
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.ServiceControl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Event), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Wait), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ServiceControlTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(ServiceControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ServiceControlTupleFields
    {
        ServiceControl,
        Name,
        Event,
        Arguments,
        Wait,
        Component_,
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

        public string ServiceControl
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.ServiceControl]?.Value;
            set => this.Set((int)ServiceControlTupleFields.ServiceControl, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.Name]?.Value;
            set => this.Set((int)ServiceControlTupleFields.Name, value);
        }

        public int Event
        {
            get => (int)this.Fields[(int)ServiceControlTupleFields.Event]?.Value;
            set => this.Set((int)ServiceControlTupleFields.Event, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.Arguments]?.Value;
            set => this.Set((int)ServiceControlTupleFields.Arguments, value);
        }

        public int Wait
        {
            get => (int)this.Fields[(int)ServiceControlTupleFields.Wait]?.Value;
            set => this.Set((int)ServiceControlTupleFields.Wait, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ServiceControlTupleFields.Component_]?.Value;
            set => this.Set((int)ServiceControlTupleFields.Component_, value);
        }
    }
}