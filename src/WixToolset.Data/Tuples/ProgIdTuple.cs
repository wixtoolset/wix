// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ProgId = new IntermediateTupleDefinition(
            TupleDefinitionType.ProgId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.ProgId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.ParentProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.ClassRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.IconRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.IconIndex), IntermediateFieldType.Number),
            },
            typeof(ProgIdTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ProgIdTupleFields
    {
        ProgId,
        ParentProgIdRef,
        ClassRef,
        Description,
        IconRef,
        IconIndex,
    }

    public class ProgIdTuple : IntermediateTuple
    {
        public ProgIdTuple() : base(TupleDefinitions.ProgId, null, null)
        {
        }

        public ProgIdTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ProgId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ProgIdTupleFields index] => this.Fields[(int)index];

        public string ProgId
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.ProgId];
            set => this.Set((int)ProgIdTupleFields.ProgId, value);
        }

        public string ParentProgIdRef
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.ParentProgIdRef];
            set => this.Set((int)ProgIdTupleFields.ParentProgIdRef, value);
        }

        public string ClassRef
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.ClassRef];
            set => this.Set((int)ProgIdTupleFields.ClassRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.Description];
            set => this.Set((int)ProgIdTupleFields.Description, value);
        }

        public string IconRef
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.IconRef];
            set => this.Set((int)ProgIdTupleFields.IconRef, value);
        }

        public int? IconIndex
        {
            get => (int?)this.Fields[(int)ProgIdTupleFields.IconIndex];
            set => this.Set((int)ProgIdTupleFields.IconIndex, value);
        }
    }
}