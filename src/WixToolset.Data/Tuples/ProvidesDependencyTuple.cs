// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ProvidesDependency = new IntermediateTupleDefinition(
            TupleDefinitionType.ProvidesDependency,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ProvidesDependencyTupleFields.Imported), IntermediateFieldType.Bool),
            },
            typeof(ProvidesDependencyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ProvidesDependencyTupleFields
    {
        PackageRef,
        Key,
        Version,
        DisplayName,
        Attributes,
        Imported,
    }

    public class ProvidesDependencyTuple : IntermediateTuple
    {
        public ProvidesDependencyTuple() : base(TupleDefinitions.ProvidesDependency, null, null)
        {
        }

        public ProvidesDependencyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ProvidesDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ProvidesDependencyTupleFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)ProvidesDependencyTupleFields.PackageRef];
            set => this.Set((int)ProvidesDependencyTupleFields.PackageRef, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)ProvidesDependencyTupleFields.Key];
            set => this.Set((int)ProvidesDependencyTupleFields.Key, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)ProvidesDependencyTupleFields.Version];
            set => this.Set((int)ProvidesDependencyTupleFields.Version, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ProvidesDependencyTupleFields.DisplayName];
            set => this.Set((int)ProvidesDependencyTupleFields.DisplayName, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)ProvidesDependencyTupleFields.Attributes];
            set => this.Set((int)ProvidesDependencyTupleFields.Attributes, value);
        }

        public bool Imported
        {
            get => (bool)this.Fields[(int)ProvidesDependencyTupleFields.Imported];
            set => this.Set((int)ProvidesDependencyTupleFields.Imported, value);
        }
    }
}
