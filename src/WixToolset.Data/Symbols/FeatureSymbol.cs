// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Feature = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Feature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.ParentFeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.Display), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.DisallowAbsent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.DisallowAdvertise), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.InstallDefault), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureSymbolFields.TypicalDefault), IntermediateFieldType.Number),
            },
            typeof(FeatureSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum FeatureSymbolFields
    {
        ParentFeatureRef,
        Title,
        Description,
        Display,
        Level,
        DirectoryRef,
        DisallowAbsent,
        DisallowAdvertise,
        InstallDefault,
        TypicalDefault,
    }

    public enum FeatureInstallDefault
    {
        Local,
        Source,
        FollowParent,
    }

    public enum FeatureTypicalDefault
    {
        Install,
        Advertise
    }

    public class FeatureSymbol : IntermediateSymbol
    {
        public FeatureSymbol() : base(SymbolDefinitions.Feature, null, null)
        {
        }

        public FeatureSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Feature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FeatureSymbolFields index] => this.Fields[(int)index];

        public string ParentFeatureRef
        {
            get => (string)this.Fields[(int)FeatureSymbolFields.ParentFeatureRef];
            set => this.Set((int)FeatureSymbolFields.ParentFeatureRef, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)FeatureSymbolFields.Title];
            set => this.Set((int)FeatureSymbolFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)FeatureSymbolFields.Description];
            set => this.Set((int)FeatureSymbolFields.Description, value);
        }

        public int Display
        {
            get => (int)this.Fields[(int)FeatureSymbolFields.Display];
            set => this.Set((int)FeatureSymbolFields.Display, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)FeatureSymbolFields.Level];
            set => this.Set((int)FeatureSymbolFields.Level, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)FeatureSymbolFields.DirectoryRef];
            set => this.Set((int)FeatureSymbolFields.DirectoryRef, value);
        }

        public bool DisallowAbsent
        {
            get => this.Fields[(int)FeatureSymbolFields.DisallowAbsent].AsBool();
            set => this.Set((int)FeatureSymbolFields.DisallowAbsent, value);
        }

        public bool DisallowAdvertise
        {
            get => this.Fields[(int)FeatureSymbolFields.DisallowAdvertise].AsBool();
            set => this.Set((int)FeatureSymbolFields.DisallowAdvertise, value);
        }

        public FeatureInstallDefault InstallDefault
        {
            get => (FeatureInstallDefault)this.Fields[(int)FeatureSymbolFields.InstallDefault].AsNumber();
            set => this.Set((int)FeatureSymbolFields.InstallDefault, (int)value);
        }

        public FeatureTypicalDefault TypicalDefault
        {
            get => (FeatureTypicalDefault)this.Fields[(int)FeatureSymbolFields.TypicalDefault].AsNumber();
            set => this.Set((int)FeatureSymbolFields.TypicalDefault, (int)value);
        }
    }
}