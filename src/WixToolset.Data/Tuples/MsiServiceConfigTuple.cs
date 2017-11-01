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
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.MsiServiceConfig), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Event), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.ConfigType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiServiceConfigTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(MsiServiceConfigTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiServiceConfigTupleFields
    {
        MsiServiceConfig,
        Name,
        Event,
        ConfigType,
        Argument,
        Component_,
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

        public string MsiServiceConfig
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.MsiServiceConfig]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.MsiServiceConfig, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.Name]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.Name, value);
        }

        public int Event
        {
            get => (int)this.Fields[(int)MsiServiceConfigTupleFields.Event]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.Event, value);
        }

        public int ConfigType
        {
            get => (int)this.Fields[(int)MsiServiceConfigTupleFields.ConfigType]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.ConfigType, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.Argument]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.Argument, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)MsiServiceConfigTupleFields.Component_]?.Value;
            set => this.Set((int)MsiServiceConfigTupleFields.Component_, value);
        }
    }
}