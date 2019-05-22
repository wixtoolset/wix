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
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnReinstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.ResetPeriod), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.RebootMessage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Command), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.Actions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.DelayActions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigFailureActionsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiServiceConfigFailureActionsTupleFields
    {
        Name,
        OnInstall,
        OnReinstall,
        OnUninstall,
        ResetPeriod,
        RebootMessage,
        Command,
        Actions,
        DelayActions,
        ComponentRef,
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

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Name];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Name, value);
        }

        public bool OnInstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.OnInstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.OnInstall, value);
        }

        public bool OnReinstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.OnReinstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.OnReinstall, value);
        }

        public bool OnUninstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.OnUninstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.OnUninstall, value);
        }

        public int? ResetPeriod
        {
            get => (int)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.ResetPeriod].AsNullableNumber();
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.ResetPeriod, value);
        }

        public string RebootMessage
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.RebootMessage];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.RebootMessage, value);
        }

        public string Command
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Command];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Command, value);
        }

        public string Actions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.Actions];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.Actions, value);
        }

        public string DelayActions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.DelayActions];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.DelayActions, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsTupleFields.ComponentRef];
            set => this.Set((int)MsiServiceConfigFailureActionsTupleFields.ComponentRef, value);
        }
    }
}