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
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Feature_Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Display), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.DisallowAbsent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.DisallowAdvertise), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.InstallDefault), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FeatureTupleFields.TypicalDefault), IntermediateFieldType.Number),
            },
            typeof(FeatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FeatureTupleFields
    {
        Feature_Parent,
        Title,
        Description,
        Display,
        Level,
        Directory_,
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

    public class FeatureTuple : IntermediateTuple
    {
        public FeatureTuple() : base(TupleDefinitions.Feature, null, null)
        {
        }

        public FeatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Feature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FeatureTupleFields index] => this.Fields[(int)index];

        public string Feature_Parent
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Feature_Parent];
            set => this.Set((int)FeatureTupleFields.Feature_Parent, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Title];
            set => this.Set((int)FeatureTupleFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Description];
            set => this.Set((int)FeatureTupleFields.Description, value);
        }

        public int Display
        {
            get => (int)this.Fields[(int)FeatureTupleFields.Display];
            set => this.Set((int)FeatureTupleFields.Display, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)FeatureTupleFields.Level];
            set => this.Set((int)FeatureTupleFields.Level, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)FeatureTupleFields.Directory_];
            set => this.Set((int)FeatureTupleFields.Directory_, value);
        }

        public bool DisallowAbsent
        {
            get => this.Fields[(int)FeatureTupleFields.DisallowAbsent].AsBool();
            set => this.Set((int)FeatureTupleFields.DisallowAbsent, value);
        }

        public bool DisallowAdvertise
        {
            get => this.Fields[(int)FeatureTupleFields.DisallowAdvertise].AsBool();
            set => this.Set((int)FeatureTupleFields.DisallowAdvertise, value);
        }

        public FeatureInstallDefault InstallDefault
        {
            get => (FeatureInstallDefault)this.Fields[(int)FeatureTupleFields.InstallDefault].AsNumber();
            set => this.Set((int)FeatureTupleFields.InstallDefault, (int)value);
        }

        public FeatureTypicalDefault TypicalDefault
        {
            get => (FeatureTypicalDefault)this.Fields[(int)FeatureTupleFields.TypicalDefault].AsNumber();
            set => this.Set((int)FeatureTupleFields.TypicalDefault, (int)value);
        }
    }
}