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
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Format), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.ContextData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.DefaultValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.KeyNoOrphan), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationTupleFields.NonNullable), IntermediateFieldType.Bool),
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
        Format,
        Type,
        ContextData,
        DefaultValue,
        KeyNoOrphan,
        NonNullable,
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

        public int Format
        {
            get => (int)this.Fields[(int)ModuleConfigurationTupleFields.Format];
            set => this.Set((int)ModuleConfigurationTupleFields.Format, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.Type];
            set => this.Set((int)ModuleConfigurationTupleFields.Type, value);
        }

        public string ContextData
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.ContextData];
            set => this.Set((int)ModuleConfigurationTupleFields.ContextData, value);
        }

        public string DefaultValue
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.DefaultValue];
            set => this.Set((int)ModuleConfigurationTupleFields.DefaultValue, value);
        }

        public bool KeyNoOrphan
        {
            get => this.Fields[(int)ModuleConfigurationTupleFields.KeyNoOrphan].AsBool();
            set => this.Set((int)ModuleConfigurationTupleFields.KeyNoOrphan, value);
        }

        public bool NonNullable
        {
            get => this.Fields[(int)ModuleConfigurationTupleFields.NonNullable].AsBool();
            set => this.Set((int)ModuleConfigurationTupleFields.NonNullable, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.DisplayName];
            set => this.Set((int)ModuleConfigurationTupleFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.Description];
            set => this.Set((int)ModuleConfigurationTupleFields.Description, value);
        }

        public string HelpLocation
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.HelpLocation];
            set => this.Set((int)ModuleConfigurationTupleFields.HelpLocation, value);
        }

        public string HelpKeyword
        {
            get => (string)this.Fields[(int)ModuleConfigurationTupleFields.HelpKeyword];
            set => this.Set((int)ModuleConfigurationTupleFields.HelpKeyword, value);
        }
    }
}