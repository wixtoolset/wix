// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Upgrade = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Upgrade,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.VersionMin), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.VersionMax), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.ExcludeLanguages), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.IgnoreRemoveFailures), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.MigrateFeatures), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.OnlyDetect), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.VersionMaxInclusive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.VersionMinInclusive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.Remove), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradeSymbolFields.ActionProperty), IntermediateFieldType.String),
            },
            typeof(UpgradeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum UpgradeSymbolFields
    {
        UpgradeCode,
        VersionMin,
        VersionMax,
        Language,
        ExcludeLanguages,
        IgnoreRemoveFailures,
        MigrateFeatures,
        OnlyDetect,
        VersionMaxInclusive,
        VersionMinInclusive,
        Remove,
        ActionProperty,
    }

    public class UpgradeSymbol : IntermediateSymbol
    {
        public UpgradeSymbol() : base(SymbolDefinitions.Upgrade, null, null)
        {
        }

        public UpgradeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Upgrade, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradeSymbolFields index] => this.Fields[(int)index];

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.UpgradeCode];
            set => this.Set((int)UpgradeSymbolFields.UpgradeCode, value);
        }

        public string VersionMin
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.VersionMin];
            set => this.Set((int)UpgradeSymbolFields.VersionMin, value);
        }

        public string VersionMax
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.VersionMax];
            set => this.Set((int)UpgradeSymbolFields.VersionMax, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.Language];
            set => this.Set((int)UpgradeSymbolFields.Language, value);
        }

        public bool ExcludeLanguages
        {
            get => this.Fields[(int)UpgradeSymbolFields.ExcludeLanguages].AsBool();
            set => this.Set((int)UpgradeSymbolFields.ExcludeLanguages, value);
        }

        public bool IgnoreRemoveFailures
        {
            get => this.Fields[(int)UpgradeSymbolFields.IgnoreRemoveFailures].AsBool();
            set => this.Set((int)UpgradeSymbolFields.IgnoreRemoveFailures, value);
        }

        public bool MigrateFeatures
        {
            get => this.Fields[(int)UpgradeSymbolFields.MigrateFeatures].AsBool();
            set => this.Set((int)UpgradeSymbolFields.MigrateFeatures, value);
        }

        public bool OnlyDetect
        {
            get => this.Fields[(int)UpgradeSymbolFields.OnlyDetect].AsBool();
            set => this.Set((int)UpgradeSymbolFields.OnlyDetect, value);
        }

        public bool VersionMaxInclusive
        {
            get => this.Fields[(int)UpgradeSymbolFields.VersionMaxInclusive].AsBool();
            set => this.Set((int)UpgradeSymbolFields.VersionMaxInclusive, value);
        }

        public bool VersionMinInclusive
        {
            get => this.Fields[(int)UpgradeSymbolFields.VersionMinInclusive].AsBool();
            set => this.Set((int)UpgradeSymbolFields.VersionMinInclusive, value);
        }

        public string Remove
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.Remove];
            set => this.Set((int)UpgradeSymbolFields.Remove, value);
        }

        public string ActionProperty
        {
            get => (string)this.Fields[(int)UpgradeSymbolFields.ActionProperty];
            set => this.Set((int)UpgradeSymbolFields.ActionProperty, value);
        }
    }
}