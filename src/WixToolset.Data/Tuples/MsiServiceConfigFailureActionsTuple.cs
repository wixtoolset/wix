// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiServiceConfigFailureActions = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiServiceConfigFailureActions,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.MsiServiceConfigFailureActions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Event), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.ResetPeriod), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.RebootMessage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Command), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Actions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.DelayActions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigFailureActionsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiServiceConfigFailureActionsTupleFields
    {
        MsiServiceConfigFailureActions,
        Name,
        Event,
        ResetPeriod,
        RebootMessage,
        Command,
        Actions,
        DelayActions,
        Component_,
    }

    public class MsiServiceConfigFailureActionsTuple : IntermediateTuple
    {
        public MsiServiceConfigFailureActionsTuple() : base(TupleDefinitions.MsiServiceConfigFailureActions, null, null)
        {
        }

        public MsiServiceConfigFailureActionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiServiceConfigFailureActions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiServiceConfigFailureActionsTupleFields index] => this.Fields[(int)index];

        public string MsiServiceConfigFailureActions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.MsiServiceConfigFailureActions]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.MsiServiceConfigFailureActions, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Name]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Name, value);
        }

        public int Event
        {
            get => (int)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Event]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Event, value);
        }

        public int ResetPeriod
        {
            get => (int)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.ResetPeriod]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.ResetPeriod, value);
        }

        public string RebootMessage
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.RebootMessage]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.RebootMessage, value);
        }

        public string Command
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Command]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Command, value);
        }

        public string Actions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Actions]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Actions, value);
        }

        public string DelayActions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.DelayActions]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.DelayActions, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Component_]?.Value;
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Component_, value);
        }
    }
}