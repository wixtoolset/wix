// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleConfiguration = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleConfiguration,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Format), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.ContextData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.DefaultValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.HelpLocation), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.HelpKeyword), IntermediateFieldType.String),
            },
            typeof(ModuleConfigurationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleConfigurationTupleFields
    {
        Name,
        Format,
        Type,
        ContextData,
        DefaultValue,
        Attributes,
        DisplayName,
        Description,
        HelpLocation,
        HelpKeyword,
    }

    public class ModuleConfigurationTuple : IntermediateTuple
    {
        public ModuleConfigurationTuple() : base(TupleDefinitions.ModuleConfiguration, null, null)
        {
        }

        public ModuleConfigurationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleConfiguration, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleConfigurationTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.Name]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.Name, value);
        }

        public int Format
        {
            get => (int)this.Fields[(int)ModuleConfigurationTupleFields.Format]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.Format, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.Type]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.Type, value);
        }

        public string ContextData
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.ContextData]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.ContextData, value);
        }

        public string DefaultValue
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.DefaultValue]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.DefaultValue, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)ModuleConfigurationTupleFields.Attributes]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.Attributes, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.DisplayName]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.Description]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.Description, value);
        }

        public string HelpLocation
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.HelpLocation]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.HelpLocation, value);
        }

        public string HelpKeyword
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.HelpKeyword]?.Value;
            set => this.Set((int)ModuleConfigurationTupleFields.HelpKeyword, value);
        }
    }
}