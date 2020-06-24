// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiServiceConfigFailureActions = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiServiceConfigFailureActions,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnReinstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.ResetPeriod), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.RebootMessage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.Command), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.Actions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.DelayActions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigFailureActionsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiServiceConfigFailureActionsSymbolFields
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

    public class MsiServiceConfigFailureActionsSymbol : IntermediateSymbol
    {
        public MsiServiceConfigFailureActionsSymbol() : base(SymbolDefinitions.MsiServiceConfigFailureActions, null, null)
        {
        }

        public MsiServiceConfigFailureActionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiServiceConfigFailureActions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiServiceConfigFailureActionsSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.Name];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.Name, value);
        }

        public bool OnInstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.OnInstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.OnInstall, value);
        }

        public bool OnReinstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.OnReinstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.OnReinstall, value);
        }

        public bool OnUninstall
        {
            get => this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.OnUninstall].AsBool();
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.OnUninstall, value);
        }

        public int? ResetPeriod
        {
            get => (int?)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.ResetPeriod];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.ResetPeriod, value);
        }

        public string RebootMessage
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.RebootMessage];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.RebootMessage, value);
        }

        public string Command
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.Command];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.Command, value);
        }

        public string Actions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.Actions];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.Actions, value);
        }

        public string DelayActions
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.DelayActions];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.DelayActions, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiServiceConfigFailureActionsSymbolFields.ComponentRef];
            set => this.Set((int)MsiServiceConfigFailureActionsSymbolFields.ComponentRef, value);
        }
    }
}