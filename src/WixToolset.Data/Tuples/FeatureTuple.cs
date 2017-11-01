// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Feature = new IntermediateTupleDefinition(
            TupleDefinitionType.Feature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Feature), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Feature_Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Display), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(FeatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FeatureTupleFields
    {
        Feature,
        Feature_Parent,
        Title,
        Description,
        Display,
        Level,
        Directory_,
        Attributes,
    }

    public class FeatureTuple : IntermediateTuple
    {
        public FeatureTuple() : base(TupleDefinitions.Feature, null, null)
        {
        }

        public FeatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Feature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FeatureTupleFields index] => this.Fields[(int)index];

        public string Feature
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Feature]?.Value;
            set => this.Set((int)FeatureTupleFields.Feature, value);
        }

        public string Feature_Parent
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Feature_Parent]?.Value;
            set => this.Set((int)FeatureTupleFields.Feature_Parent, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Title]?.Value;
            set => this.Set((int)FeatureTupleFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Description]?.Value;
            set => this.Set((int)FeatureTupleFields.Description, value);
        }

        public int Display
        {
            get => (int)this.Fields[(int)FeatureTupleFields.Display]?.Value;
            set => this.Set((int)FeatureTupleFields.Display, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)FeatureTupleFields.Level]?.Value;
            set => this.Set((int)FeatureTupleFields.Level, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Directory_]?.Value;
            set => this.Set((int)FeatureTupleFields.Directory_, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)FeatureTupleFields.Attributes]?.Value;
            set => this.Set((int)FeatureTupleFields.Attributes, value);
        }
    }
}