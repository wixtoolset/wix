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
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.LibID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Version), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Feature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibTupleFields.Cost), IntermediateFieldType.Number),
            },
            typeof(TypeLibTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TypeLibTupleFields
    {
        LibID,
        Language,
        Component_,
        Version,
        Description,
        Directory_,
        Feature_,
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

        public string LibID
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.LibID]?.Value;
            set => this.Set((int)TypeLibTupleFields.LibID, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Language]?.Value;
            set => this.Set((int)TypeLibTupleFields.Language, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.Component_]?.Value;
            set => this.Set((int)TypeLibTupleFields.Component_, value);
        }

        public int Version
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Version]?.Value;
            set => this.Set((int)TypeLibTupleFields.Version, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.Description]?.Value;
            set => this.Set((int)TypeLibTupleFields.Description, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.Directory_]?.Value;
            set => this.Set((int)TypeLibTupleFields.Directory_, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)TypeLibTupleFields.Feature_]?.Value;
            set => this.Set((int)TypeLibTupleFields.Feature_, value);
        }

        public int Cost
        {
            get => (int)this.Fields[(int)TypeLibTupleFields.Cost]?.Value;
            set => this.Set((int)TypeLibTupleFields.Cost, value);
        }
    }
}