// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPackageFeatureInfo = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPackageFeatureInfo,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Package), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Feature), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Size), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Display), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Level), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Directory), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageFeatureInfoTupleFields.Attributes), IntermediateFieldType.String),
            },
            typeof(WixPackageFeatureInfoTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPackageFeatureInfoTupleFields
    {
        Package,
        Feature,
        Size,
        Parent,
        Title,
        Description,
        Display,
        Level,
        Directory,
        Attributes,
    }

    public class WixPackageFeatureInfoTuple : IntermediateTuple
    {
        public WixPackageFeatureInfoTuple() : base(TupleDefinitions.WixPackageFeatureInfo, null, null)
        {
        }

        public WixPackageFeatureInfoTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPackageFeatureInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPackageFeatureInfoTupleFields index] => this.Fields[(int)index];

        public string Package
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Package];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Package, value);
        }

        public string Feature
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Feature];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Feature, value);
        }

        public string Size
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Size];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Size, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Parent];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Parent, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Title];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Description];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Description, value);
        }

        public string Display
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Display];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Display, value);
        }

        public string Level
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Level];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Level, value);
        }

        public string Directory
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Directory];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Directory, value);
        }

        public string Attributes
        {
            get => (string)this.Fields[(int)WixPackageFeatureInfoTupleFields.Attributes];
            set => this.Set((int)WixPackageFeatureInfoTupleFields.Attributes, value);
        }
    }
}