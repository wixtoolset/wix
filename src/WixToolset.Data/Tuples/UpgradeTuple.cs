// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Upgrade = new IntermediateTupleDefinition(
            TupleDefinitionType.Upgrade,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.VersionMin), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.VersionMax), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.Remove), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeTupleFields.ActionProperty), IntermediateFieldType.String),
            },
            typeof(UpgradeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UpgradeTupleFields
    {
        UpgradeCode,
        VersionMin,
        VersionMax,
        Language,
        Attributes,
        Remove,
        ActionProperty,
    }

    public class UpgradeTuple : IntermediateTuple
    {
        public UpgradeTuple() : base(TupleDefinitions.Upgrade, null, null)
        {
        }

        public UpgradeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Upgrade, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradeTupleFields index] => this.Fields[(int)index];

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.UpgradeCode]?.Value;
            set => this.Set((int)UpgradeTupleFields.UpgradeCode, value);
        }

        public string VersionMin
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.VersionMin]?.Value;
            set => this.Set((int)UpgradeTupleFields.VersionMin, value);
        }

        public string VersionMax
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.VersionMax]?.Value;
            set => this.Set((int)UpgradeTupleFields.VersionMax, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.Language]?.Value;
            set => this.Set((int)UpgradeTupleFields.Language, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)UpgradeTupleFields.Attributes]?.Value;
            set => this.Set((int)UpgradeTupleFields.Attributes, value);
        }

        public string Remove
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.Remove]?.Value;
            set => this.Set((int)UpgradeTupleFields.Remove, value);
        }

        public string ActionProperty
        {
            get => (string)this.Fields[(int)UpgradeTupleFields.ActionProperty]?.Value;
            set => this.Set((int)UpgradeTupleFields.ActionProperty, value);
        }
    }
}