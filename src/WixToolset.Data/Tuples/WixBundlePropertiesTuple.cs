// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleProperties = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleProperties,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.Compressed), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.Id), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePropertiesTupleFields.PerMachine), IntermediateFieldType.String),
            },
            typeof(WixBundlePropertiesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePropertiesTupleFields
    {
        DisplayName,
        LogPathVariable,
        Compressed,
        Id,
        UpgradeCode,
        PerMachine,
    }

    public class WixBundlePropertiesTuple : IntermediateTuple
    {
        public WixBundlePropertiesTuple() : base(TupleDefinitions.WixBundleProperties, null, null)
        {
        }

        public WixBundlePropertiesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleProperties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePropertiesTupleFields index] => this.Fields[(int)index];

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.DisplayName];
            set => this.Set((int)WixBundlePropertiesTupleFields.DisplayName, value);
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.LogPathVariable];
            set => this.Set((int)WixBundlePropertiesTupleFields.LogPathVariable, value);
        }

        public string Compressed
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.Compressed];
            set => this.Set((int)WixBundlePropertiesTupleFields.Compressed, value);
        }

        public string Id
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.Id];
            set => this.Set((int)WixBundlePropertiesTupleFields.Id, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.UpgradeCode];
            set => this.Set((int)WixBundlePropertiesTupleFields.UpgradeCode, value);
        }

        public string PerMachine
        {
            get => (string)this.Fields[(int)WixBundlePropertiesTupleFields.PerMachine];
            set => this.Set((int)WixBundlePropertiesTupleFields.PerMachine, value);
        }
    }
}