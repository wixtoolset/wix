// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleMsiFeature = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleMsiFeature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.WixBundlePackage_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Size), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Display), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Directory), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleMsiFeatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleMsiFeatureTupleFields
    {
        WixBundlePackage_,
        Name,
        Size,
        Parent,
        Title,
        Description,
        Display,
        Level,
        Directory,
        Attributes,
    }

    public class WixBundleMsiFeatureTuple : IntermediateTuple
    {
        public WixBundleMsiFeatureTuple() : base(TupleDefinitions.WixBundleMsiFeature, null, null)
        {
        }

        public WixBundleMsiFeatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleMsiFeature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiFeatureTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackage_
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.WixBundlePackage_]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.WixBundlePackage_, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.Name]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Name, value);
        }

        public int Size
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureTupleFields.Size]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Size, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.Parent]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Parent, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.Title]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.Description]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Description, value);
        }

        public int Display
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureTupleFields.Display]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Display, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureTupleFields.Level]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Level, value);
        }

        public string Directory
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureTupleFields.Directory]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Directory, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureTupleFields.Attributes]?.Value;
            set => this.Set((int)WixBundleMsiFeatureTupleFields.Attributes, value);
        }
    }
}