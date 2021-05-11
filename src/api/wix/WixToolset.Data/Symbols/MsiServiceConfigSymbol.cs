// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiServiceConfig = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiServiceConfig,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiServiceConfigSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnReinstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsSymbolFields.OnUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigSymbolFields.ConfigType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigSymbolFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiServiceConfigSymbolFields
    {
        Name,
        OnInstall,
        OnReinstall,
        OnUninstall,
        ConfigType,
        Argument,
        ComponentRef,
    }

    public enum MsiServiceConfigType
    {
        DelayedAutoStart = 3,
        FailureActionsFlag,
        ServiceSidInfo,
        RequiredPrivilegesInfo,
        PreshutdownInfo,
    }

    public class MsiServiceConfigSymbol : IntermediateSymbol
    {
        public MsiServiceConfigSymbol() : base(SymbolDefinitions.MsiServiceConfig, null, null)
        {
        }

        public MsiServiceConfigSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiServiceConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiServiceConfigSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigSymbolFields.Name];
            set => this.Set((int)MsiServiceConfigSymbolFields.Name, value);
        }

        public bool OnInstall
        {
            get => this.Fields[(int)MsiServiceConfigSymbolFields.OnInstall].AsBool();
            set => this.Set((int)MsiServiceConfigSymbolFields.OnInstall, value);
        }

        public bool OnReinstall
        {
            get => this.Fields[(int)MsiServiceConfigSymbolFields.OnReinstall].AsBool();
            set => this.Set((int)MsiServiceConfigSymbolFields.OnReinstall, value);
        }

        public bool OnUninstall
        {
            get => this.Fields[(int)MsiServiceConfigSymbolFields.OnUninstall].AsBool();
            set => this.Set((int)MsiServiceConfigSymbolFields.OnUninstall, value);
        }

        public MsiServiceConfigType ConfigType
        {
            get => (MsiServiceConfigType)this.Fields[(int)MsiServiceConfigSymbolFields.ConfigType].AsNumber();
            set => this.Set((int)MsiServiceConfigSymbolFields.ConfigType, (int)value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)MsiServiceConfigSymbolFields.Argument];
            set => this.Set((int)MsiServiceConfigSymbolFields.Argument, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiServiceConfigSymbolFields.ComponentRef];
            set => this.Set((int)MsiServiceConfigSymbolFields.ComponentRef, value);
        }
    }
}