// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiServiceConfig = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiServiceConfig,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnReinstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigFailureActionsTupleFields.OnUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.ConfigType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiServiceConfigTupleFields
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

    public class MsiServiceConfigTuple : IntermediateTuple
    {
        public MsiServiceConfigTuple() : base(TupleDefinitions.MsiServiceConfig, null, null)
        {
        }

        public MsiServiceConfigTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiServiceConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiServiceConfigTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.Name];
            set => this.Set((int)MsiServiceConfigTupleFields.Name, value);
        }

        public bool OnInstall
        {
            get => this.Fields[(int)MsiServiceConfigTupleFields.OnInstall].AsBool();
            set => this.Set((int)MsiServiceConfigTupleFields.OnInstall, value);
        }

        public bool OnReinstall
        {
            get => this.Fields[(int)MsiServiceConfigTupleFields.OnReinstall].AsBool();
            set => this.Set((int)MsiServiceConfigTupleFields.OnReinstall, value);
        }

        public bool OnUninstall
        {
            get => this.Fields[(int)MsiServiceConfigTupleFields.OnUninstall].AsBool();
            set => this.Set((int)MsiServiceConfigTupleFields.OnUninstall, value);
        }

        public MsiServiceConfigType ConfigType
        {
            get => (MsiServiceConfigType)this.Fields[(int)MsiServiceConfigTupleFields.ConfigType].AsNumber();
            set => this.Set((int)MsiServiceConfigTupleFields.ConfigType, (int)value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.Argument];
            set => this.Set((int)MsiServiceConfigTupleFields.Argument, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.ComponentRef];
            set => this.Set((int)MsiServiceConfigTupleFields.ComponentRef, value);
        }
    }
}