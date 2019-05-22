// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition TypeLib = new IntermediateTupleDefinition(
            TupleDefinitionType.TypeLib,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.LibId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Version), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Cost), IntermediateFieldType.Number),
            },
            typeof(TypeLibTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TypeLibTupleFields
    {
        LibId,
        Language,
        ComponentRef,
        Version,
        Description,
        DirectoryRef,
        FeatureRef,
        Cost,
    }

    public class TypeLibTuple : IntermediateTuple
    {
        public TypeLibTuple() : base(TupleDefinitions.TypeLib, null, null)
        {
        }

        public TypeLibTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.TypeLib, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TypeLibTupleFields index] => this.Fields[(int)index];

        public string LibId
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.LibId];
            set => this.Set((int)TypeLibTupleFields.LibId, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Language];
            set => this.Set((int)TypeLibTupleFields.Language, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.ComponentRef];
            set => this.Set((int)TypeLibTupleFields.ComponentRef, value);
        }

        public int Version
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Version];
            set => this.Set((int)TypeLibTupleFields.Version, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.Description];
            set => this.Set((int)TypeLibTupleFields.Description, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.DirectoryRef];
            set => this.Set((int)TypeLibTupleFields.DirectoryRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.FeatureRef];
            set => this.Set((int)TypeLibTupleFields.FeatureRef, value);
        }

        public int Cost
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Cost];
            set => this.Set((int)TypeLibTupleFields.Cost, value);
        }
    }
}
